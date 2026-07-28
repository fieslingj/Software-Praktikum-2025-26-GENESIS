using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Arch.Core;
using Genesis.Architecture;
using Genesis.Architecture.Audio;
using Genesis.Gameplay.Components;
using Genesis.Gameplay.Definitions;
using Genesis.Gameplay.Generators;
using Genesis.Gameplay.Systems;
using Genesis.GameStates.Core;
using Genesis.Simulation.LoadingTasks;
using Genesis.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Genesis.GameStates.Menus;

public class ChooseCharacterState : IGameState
{
    private GameStateManager mStateManager;
    private GameServices mServices;
    private ScreenService mScreenService;
    private RandomService mRandomService;

    private World mUiWorld;

    private Texture2D mPixelTexture;
    private Texture2D mGameLogo;
    private SpriteFont mFont;
    private TooltipRenderer mTooltipRenderer;
    
    private List<Entity> mPanelEntities = [];
    private MutantType? mSelectedCharacter;

    private List<Entity> mActiveTooltipEntities = [];
    private Entity mSpecialAbilityIconEntity = Entity.Null;
    
    private const float PaddingY = 20f;
    private const float PaddingX = 20f;
    
    private RenderTarget2D mChartRenderTarget;
    private BasicEffect mChartEffect;
    
    private const int ChartTextureSize = 256;

    private readonly MutantType[] mButtonMappings = 
    { 
        MutantType.Mutant1, 
        MutantType.Mutant2, 
        MutantType.Mutant3,
    };
    
    private float mMaxRefSpeed;
    private float mMaxRefMass;
    private float mMaxRefHealth;
    private float mMaxRefStamina;

    private const float ChartRadius = 60f;
    private const int CircleSegments = 64;
    
    public void Initialize(GameStateManager manager, GameServices services, ScreenService screen, AudioService sound)
    {
        mStateManager = manager;
        mServices = services;
        mScreenService = screen;
        mRandomService = new RandomService(Environment.TickCount);
        mTooltipRenderer = new TooltipRenderer(services, screen);
        mFont = services.Content.Load<SpriteFont>("Fonts/HudFont");
        try {
            mGameLogo = services.Content.Load<Texture2D>("Sprites/Icons/logo"); 
        } catch { Console.WriteLine("Could not load Logo"); }

        InitializeChartResources();
        
        mMaxRefSpeed = 1f;
        mMaxRefMass = 1f;
        mMaxRefHealth = 1f;
        mMaxRefStamina = 1f;

        foreach (var type in mButtonMappings)
        {
            var def = PlayerDefinitions.Get(type);

            if (def.MovementSpeed > mMaxRefSpeed) {mMaxRefSpeed = def.MovementSpeed;}
            if (def.Mass > mMaxRefMass) {mMaxRefMass = def.Mass;}
            if (def.MaxHealth > mMaxRefHealth) {mMaxRefHealth = (float)def.MaxHealth;}
            if (def.MaxStamina > mMaxRefStamina) {mMaxRefStamina = (float)def.MaxStamina;}
        }
    }

    public void Enter()
    {
        mUiWorld = World.Create();
        mPixelTexture = new Texture2D(mScreenService.Graphics, 1, 1);
        mPixelTexture.SetData([Color.White]);
        BuildUi();
        UpdatePreviewPanel();
    }

    public void Exit()
    {
        mUiWorld.Dispose();
        mActiveTooltipEntities.Clear();
        mTooltipRenderer?.Dispose();
        mTooltipRenderer = null;
        mPixelTexture?.Dispose();
        mPixelTexture = null;
    }

    public void Pause() { }

    public void Resume() { }

    public void HandleInput(InputService input)
    {
        if (input.IsActionPressed(InputAction.Pause)) { mStateManager.PopState(); return; }
        
        var rawMousePos = input.GetMousePosition();
        var virtualMousePoint = mScreenService.Adapter.PointToScreen(rawMousePos.X, rawMousePos.Y);
        var mousePos = virtualMousePoint.ToVector2();
        HandleSpecialItemHover(mousePos);
        
        // Run input systems
        mServices.Systems.HandleInput(mUiWorld, input);
    }

    public void Update(GameTime gameTime) { }

    public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        var uiScale = mScreenService.GetUiScale();
        spriteBatch.Begin(
            samplerState: SamplerState.PointClamp,
            transformMatrix: Matrix.CreateScale(uiScale, uiScale, 1.0f),
            sortMode: SpriteSortMode.FrontToBack
        );
        
        mServices.Systems.Get<DrawSystem>().Draw(mUiWorld, spriteBatch);
        spriteBatch.End();
    }

    private void BuildUi()
    {
        const int buttonCount = 5;
        
        const float virtualWidth = ScreenService.VirtualWidth;
        const float virtualHeight = ScreenService.VirtualHeight;
        
        // Set the button positions
        const float gap = virtualHeight / 7f;
        
        const float positionX = virtualWidth * 0.2f;
        const float positionY = (virtualHeight - (buttonCount - 1) * gap) / 2f;

        // Button size settings
        const float buttonWidth = (virtualWidth / 4f);
        const float buttonHeight = (virtualWidth / 20f);
        const float paddingX = (virtualWidth / 80f);
        const float paddingY = (virtualWidth / 80f);
            
        var targetPixels = new Rectangle(0, 0, (int)buttonWidth, (int)buttonHeight);
        var padding = new Point((int)paddingX, (int)paddingY);

        // Character buttons
        var i = 0;
        foreach (var type in Enum.GetValues<MutantType>())
        {
            var def = PlayerDefinitions.Get(type);
            var currentType = type;
            mServices.UiFactory.CreateButtonWithSprite(
                world: mUiWorld,
                position: new Vector2(positionX, positionY + gap * i),
                text: def.Name,
                onClick: () => SelectCharacter(currentType),
                targetPixels: targetPixels,
                padding: padding,
                font: mFont
            );
            i++;
        }
        
        // Return Button
        mServices.UiFactory.CreateButtonWithSprite(
            world: mUiWorld,
            position: new Vector2(positionX, positionY + gap * 4),
            text: "Return",
            onClick: () => mStateManager.PopState(),
            targetPixels: targetPixels,
            padding: padding,
            font: mFont
        );
        
        const float margin = 40f;
        const float splitRatio = 0.3f;
        
        const float panelX = (virtualWidth * splitRatio) + margin;
        const float panelWidth = (virtualWidth * (1.0f - splitRatio)) - (margin * 2);
        const float panelHeight = virtualHeight - (margin * 2);

        var bgEntity = mServices.UiFactory.MarkAsStaticUi(mUiWorld, mUiWorld.Create());
        mUiWorld.Add(bgEntity,
            new PositionComponent(new Vector2(panelX + panelWidth / 2f, margin + panelHeight / 2f)),
            new SpriteComponent(mPixelTexture, new Rectangle(0, 0, (int)panelWidth, (int)panelHeight), layerDepth: 0.2f)
            {
                mColor = new Color(20, 20, 20, 240),
                Origin = new Vector2(panelWidth / 2f, panelHeight / 2f)
            },
            new IsVisibleComponent(),
            new IgnoreCullingComponent()
        );
    }
    
    private void InitializeChartResources()
    {
        mChartRenderTarget = new RenderTarget2D(
            mScreenService.Graphics, 
            ChartTextureSize, 
            ChartTextureSize, 
            false, 
            SurfaceFormat.Color, 
            DepthFormat.None,
            8,
            RenderTargetUsage.DiscardContents);

        mChartEffect = new BasicEffect(mScreenService.Graphics)
        {
            VertexColorEnabled = true,
            View = Matrix.Identity,
            World = Matrix.Identity,
            Projection = Matrix.CreateOrthographicOffCenter(0, ChartTextureSize, ChartTextureSize, 0, 0, 1)
        };
    }

    private Texture2D GenerateRadarChartTexture(PlayerDefinition def, Color fillColor, Color outlineColor)
    {
        var gd = mScreenService.Graphics;
        var previousViewport = gd.Viewport; 
        var previousRenderTarget = gd.GetRenderTargets();

        gd.SetRenderTarget(mChartRenderTarget);
        gd.Clear(Color.Transparent);
        
        // Anti-Aliasing aktivieren
        var rasterizerState = new RasterizerState { MultiSampleAntiAlias = true, CullMode = CullMode.None };
        gd.RasterizerState = rasterizerState;

        var localCenter = new Vector2(ChartTextureSize / 2f, ChartTextureSize / 2f);
        GetRadarChartGeometry(localCenter, def, fillColor, outlineColor, out var vertices, out var borderVertices);
        
        Color outerCircleColor = Color.Gray * 0.5f; 
        var circleVertices = GetCircleGeometry(localCenter, ChartRadius, outerCircleColor);

        foreach (var pass in mChartEffect.CurrentTechnique.Passes)
        {
            pass.Apply();
            gd.DrawUserPrimitives(PrimitiveType.LineStrip, circleVertices, 0, CircleSegments);
            if (vertices.Length > 0)
            {
                gd.DrawUserPrimitives(PrimitiveType.TriangleList, vertices, 0, vertices.Length / 3);
            }
            if (borderVertices.Length > 0)
            {
                gd.DrawUserPrimitives(PrimitiveType.LineStrip, borderVertices, 0, borderVertices.Length - 1);
            }
        }

        gd.SetRenderTargets(previousRenderTarget);
        gd.Viewport = previousViewport;
        return mChartRenderTarget;
    }

    private void GetRadarChartGeometry(Vector2 center, PlayerDefinition def, Color fillColor, Color outlineColor, out VertexPositionColor[] vertices, out VertexPositionColor[] borderVertices)
    {
        var nSpeed = Math.Clamp(def.MovementSpeed / mMaxRefSpeed, 0.2f, 1f);
        var nMass = Math.Clamp(def.Mass / mMaxRefMass, 0.2f, 1f);
        var nHealth = Math.Clamp(def.MaxHealth / mMaxRefHealth, 0.2f, 1f);
        var nStamina = Math.Clamp(def.MaxStamina / mMaxRefStamina, 0.2f, 1f);

        var pCenter = ToVec3(center);
        var pSpeed = ToVec3(center + new Vector2(0, -nSpeed * ChartRadius));    
        var pMass = ToVec3(center + new Vector2(nMass * ChartRadius, 0));       
        var pHealth = ToVec3(center + new Vector2(0, nHealth * ChartRadius));   
        var pStamina = ToVec3(center + new Vector2(-nStamina * ChartRadius, 0)); 

        vertices = new VertexPositionColor[12]; 
        // Tri 1
        vertices[0] = new VertexPositionColor(pCenter, fillColor); vertices[1] = new VertexPositionColor(pSpeed, fillColor); vertices[2] = new VertexPositionColor(pMass, fillColor);
        // Tri 2
        vertices[3] = new VertexPositionColor(pCenter, fillColor); vertices[4] = new VertexPositionColor(pMass, fillColor); vertices[5] = new VertexPositionColor(pHealth, fillColor);
        // Tri 3
        vertices[6] = new VertexPositionColor(pCenter, fillColor); vertices[7] = new VertexPositionColor(pHealth, fillColor); vertices[8] = new VertexPositionColor(pStamina, fillColor);
        // Tri 4
        vertices[9] = new VertexPositionColor(pCenter, fillColor); vertices[10] = new VertexPositionColor(pStamina, fillColor); vertices[11] = new VertexPositionColor(pSpeed, fillColor);

        borderVertices = new VertexPositionColor[5];
        borderVertices[0] = new VertexPositionColor(pSpeed, outlineColor);
        borderVertices[1] = new VertexPositionColor(pMass, outlineColor);
        borderVertices[2] = new VertexPositionColor(pHealth, outlineColor);
        borderVertices[3] = new VertexPositionColor(pStamina, outlineColor);
        borderVertices[4] = new VertexPositionColor(pSpeed, outlineColor);
        return;

        Vector3 ToVec3(Vector2 v) => new(v, 0);
    }
    
    private VertexPositionColor[] GetCircleGeometry(Vector2 center, float radius, Color color)
    {
        // Wir brauchen einen Punkt mehr als Segmente, um den Kreis zu schließen (Start = Ende)
        var vertices = new VertexPositionColor[CircleSegments + 1];
        const float angleStep = MathHelper.TwoPi / CircleSegments;

        for (var i = 0; i <= CircleSegments; i++)
        {
            var angle = i * angleStep;
            var x = center.X + radius * (float)Math.Cos(angle - MathHelper.PiOver2);
            var y = center.Y + radius * (float)Math.Sin(angle - MathHelper.PiOver2);
            
            vertices[i] = new VertexPositionColor(new Vector3(x, y, 0), color);
        }
        return vertices;
    }
    
    private void HandleSpecialItemHover(Vector2 mousePos)
    {
        if (mSpecialAbilityIconEntity == Entity.Null || !mUiWorld.IsAlive(mSpecialAbilityIconEntity)) 
        {
            if (mActiveTooltipEntities.Count > 0) {ClearTooltip();}
            return; 
        }

        ref var pos = ref mUiWorld.Get<PositionComponent>(mSpecialAbilityIconEntity);
        ref var sprite = ref mUiWorld.Get<SpriteComponent>(mSpecialAbilityIconEntity);
        
        var width = sprite.SourceRect.Width * sprite.mScale;
        var height = sprite.SourceRect.Height * sprite.mScale;
        var iconRect = new Rectangle(
            (int)(pos.Value.X - width / 2f),
            (int)(pos.Value.Y - height / 2f),
            (int)width,
            (int)height
        );

        if (iconRect.Contains(mousePos))
        {
            if (mActiveTooltipEntities.Count != 0 || !mSelectedCharacter.HasValue) { return; }

            var def = PlayerDefinitions.Get(mSelectedCharacter.Value);
            var itemDef = ItemDefinitions.Get(def.SpecialItem);
                
            mActiveTooltipEntities = mTooltipRenderer.CreateItemTooltip(mUiWorld, itemDef, mousePos);
        }
        else
        {
            if (mActiveTooltipEntities.Count > 0)
            {
                ClearTooltip();
            }
        }
    }
    
    private void SelectCharacter(MutantType type)
    {
        mSelectedCharacter = type;
        UpdatePreviewPanel();
    }

    private void UpdatePreviewPanel()
    {
        foreach (var e in mPanelEntities.Where(e => mUiWorld.IsAlive(e)))
        {
            mUiWorld.Destroy(e);
        }
        mPanelEntities.Clear();
        
        // Reset Special Icon Reference
        mSpecialAbilityIconEntity = Entity.Null;
        ClearTooltip();

        if (mSelectedCharacter == null)
        {
            mPanelEntities = CreateDefaultPreview();
        }
        else
        {
            var def = PlayerDefinitions.Get(mSelectedCharacter.Value);
            mPanelEntities = CreateCharacterPanel(def, () => StartGameWithCharacter(mSelectedCharacter.Value));
        }
    }
    
    private void StartGameWithCharacter(MutantType mutantType)
    {
        var floorGenerator = new FloorGenerator(mRandomService);
        mStateManager.ChangeState(new LoadingState(new NewGameTask(floorGenerator, mutantType, mRandomService), mScreenService.Graphics));
    }
    
    public List<Entity> CreateDefaultPreview()
    {
        var entities = new List<Entity>();
        
        const float margin = 40f;
        const float splitRatio = 0.3f;
        
        const float panelX = (ScreenService.VirtualWidth * splitRatio) + margin;
        const float panelWidth = (ScreenService.VirtualWidth * (1.0f - splitRatio)) - (margin * 2);
        const float panelHeight = ScreenService.VirtualHeight - (margin * 2);
        
        var panelCenter = new Vector2(panelX + panelWidth / 2f, margin + panelHeight / 2f);

        var titlePos = new Vector2(panelCenter.X, margin + 2 * PaddingY);
        
        entities.Add(CreateTextEntity(titlePos, "NEW GAME", Color.Gold, TextAlignment.TopCenter));

        if (mGameLogo != null)
        {
            const float maxLogoWidth = panelWidth * 0.8f;
            var scale = maxLogoWidth / mGameLogo.Width;
            
            if (mGameLogo.Height * scale > panelHeight * 0.6f)
            {
                scale = (panelHeight * 0.6f) / mGameLogo.Height;
            }

            var logoEntity = mServices.UiFactory.MarkAsStaticUi(mUiWorld, mUiWorld.Create());
            mUiWorld.Add(logoEntity,
                new PositionComponent(panelCenter),
                new SpriteComponent(mGameLogo, mGameLogo.Bounds, 0.7f)
                {
                    Origin = new Vector2(mGameLogo.Width / 2f, mGameLogo.Height / 2f),
                    mScale = scale,
                    mColor = Color.White
                },
                new IsVisibleComponent(),
                new IgnoreCullingComponent()
            );
            entities.Add(logoEntity);
        }

        const string hintText = "Choose a character to start the game.";
        var hintPos = new Vector2(panelCenter.X, margin + panelHeight - PaddingY - 20);
        entities.Add(CreateTextEntity(hintPos, hintText, Color.Gray, TextAlignment.BottomCenter));

        return entities;
    }
    
    private void ClearTooltip()
    {
        foreach (var entity in mActiveTooltipEntities.Where(entity => mUiWorld.IsAlive(entity)))
        {
            mUiWorld.Destroy(entity);
        }

        mActiveTooltipEntities.Clear();
    }
    
    private List<Entity> CreateCharacterPanel(PlayerDefinition def, Action onStartClick)
    {
        var entities = new List<Entity>();
        
        const float margin = 40f;
        const float splitRatio = 0.3f;
        const float panelX = (ScreenService.VirtualWidth * splitRatio) + margin;
        const float panelWidth = (ScreenService.VirtualWidth * (1.0f - splitRatio)) - (margin * 2);
        const float panelHeight = ScreenService.VirtualHeight - (margin * 2);
        
        // Title
        var titlePos = new Vector2(panelX + PaddingX, margin + PaddingY);
        entities.Add(CreateTextEntity(titlePos, def.Name, Color.Orange, TextAlignment.TopLeft));

        // Description
        var titleH = mFont.MeasureString(def.Name).Y;
        var descPos = titlePos + new Vector2(0, titleH + 20);
        var wrappedDesc = WrapText(mFont, def.Description, panelWidth - (PaddingX * 2));
        entities.Add(CreateTextEntity(descPos, wrappedDesc, Color.LightGray, TextAlignment.TopLeft));

        // Radar Chart
        const float panelBottomY = margin + panelHeight;
        const float buttonAreaHeight = 100f;
        const float contentCenterY = panelBottomY - buttonAreaHeight - ChartRadius - 20f;
        var abilityPos = new Vector2(panelX + (panelWidth * 0.80f), contentCenterY);
        var chartCenterPos = new Vector2(panelX + (panelWidth * 0.35f), contentCenterY);

        // Special Ability
        var iconEntity = mServices.UiFactory.MarkAsStaticUi(mUiWorld, mUiWorld.Create());
        var iconTexture = mServices.ItemAssets.GetIcon(def.SpecialItem);
        const float targetIconSize = 128f;
        var iconScale = targetIconSize / iconTexture.Width;
        mUiWorld.Add(iconEntity,
            new PositionComponent(abilityPos),
            new SpriteComponent(iconTexture, iconTexture.Bounds, 0.7f)
            {
                mColor = Color.White,
                mScale = iconScale,
                Origin = new Vector2(iconTexture.Width / 2f, iconTexture.Height / 2f)
            },
            new IsVisibleComponent(),
            new IgnoreCullingComponent()
        );
        entities.Add(iconEntity);
        
        mSpecialAbilityIconEntity = iconEntity;
        entities.Add(CreateTextEntity(abilityPos - new Vector2(0, 70), "Special Ability", Color.Gold, TextAlignment.BottomCenter));

        var chartTexture = GenerateRadarChartTexture(def, Color.LightGreen * 0.5f, Color.White);

        var chartEntity = mServices.UiFactory.MarkAsStaticUi(mUiWorld, mUiWorld.Create());
        mUiWorld.Add(chartEntity, 
            new PositionComponent(chartCenterPos),
            new SpriteComponent(chartTexture, chartTexture.Bounds, 0.7f) 
            {
                Origin = new Vector2(ChartTextureSize / 2f, ChartTextureSize / 2f)
            },
            new IsVisibleComponent(),
            new IgnoreCullingComponent()
        );
        entities.Add(chartEntity);
        entities.AddRange(CreateAlignedChartLabels(chartCenterPos, def));


        const float btnWidth = 300f;
        const float btnHeight = 50f;
        const float btnPadding = 20f;

        var btnPos = new Vector2(
            panelX + panelWidth - (btnWidth / 2f) - btnPadding, 
            margin + panelHeight - (btnHeight / 2f) - btnPadding
        );

        var btnEntity = mServices.UiFactory.CreateButtonWithSprite(
            world: mUiWorld,
            position: btnPos,
            text: "START GAME",
            onClick: onStartClick,
            targetPixels: new Rectangle(0, 0, (int)btnWidth, (int)btnHeight),
            padding: new Point(10, 10),
            font: mFont
        );
        entities.Add(btnEntity);

        return entities;
    }

    private List<Entity> CreateAlignedChartLabels(Vector2 center, PlayerDefinition def)
    {
        var list = new List<Entity>();
        
        const float padding = 15f; 
        const float sideOffset = 30f;
        var lineHeight = mFont.MeasureString("A").Y;

        CreateDoubleLabel("Speed", $"{def.MovementSpeed:0}", new Vector2(0, -ChartRadius - padding), new Vector2(0, -1));
        CreateDoubleLabel("Mass", $"{def.Mass}", new Vector2(ChartRadius + padding + sideOffset, 0), new Vector2(1, 0));
        CreateDoubleLabel("Health", $"{def.MaxHealth}", new Vector2(0, ChartRadius + padding), new Vector2(0, 1));
        CreateDoubleLabel("Stamina", $"{def.MaxStamina}", new Vector2(-ChartRadius - padding - sideOffset, 0), new Vector2(-1, 0));

        return list;
        void CreateDoubleLabel(string labelText, string valueText, Vector2 pos, Vector2 direction)
        {
            Vector2 labelPos, valuePos;
            TextAlignment align;

            switch (direction.Y)
            {
                case < 0:
                    align = TextAlignment.BottomCenter;
                    valuePos = pos;
                    labelPos = pos - new Vector2(0, lineHeight * 0.9f);
                    break;
                case > 0:
                    align = TextAlignment.TopCenter;
                    labelPos = pos;
                    valuePos = pos + new Vector2(0, lineHeight * 0.9f);
                    break;
                default:
                    align = TextAlignment.TopCenter;
                
                    labelPos = pos - new Vector2(0, lineHeight * 0.8f);
                    valuePos = pos + new Vector2(0, lineHeight * 0.2f);
                    break;
            }

            list.Add(CreateTextEntity(center + labelPos, labelText, Color.Gray, align));
            list.Add(CreateTextEntity(center + valuePos, valueText, Color.White, align));
        }
    }

    private Entity CreateTextEntity(Vector2 pos, string text, Color color, TextAlignment align)
    {
        var e = mServices.UiFactory.MarkAsStaticUi(mUiWorld, mUiWorld.Create());
        mUiWorld.Add(e,
            new PositionComponent(pos),
            new TextComponent(text, mFont, color, align, 0.8f),
            new IsVisibleComponent(),
            new IgnoreCullingComponent()
        );
        return e;
    }
    
    private static string WrapText(SpriteFont font, string text, float maxWidth)
    {
        if (string.IsNullOrEmpty(text)) { return string.Empty; }
        var words = text.Split(' ');
        var sb = new StringBuilder();
        var lineWidth = 0f;
        var spaceWidth = font.MeasureString(" ").X;
        foreach (var word in words)
        {
            var size = font.MeasureString(word);
            if (lineWidth + size.X < maxWidth) { sb.Append(word + " "); lineWidth += size.X + spaceWidth; }
            else { sb.Append("\n" + word + " "); lineWidth = size.X + spaceWidth; }
        }
        return sb.ToString().TrimEnd();
    }
}