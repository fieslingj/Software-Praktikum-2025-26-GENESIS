using System;
using System.Collections.Generic;
using Arch.Core;
using Genesis.Architecture;
using Genesis.Architecture.Audio;
using Genesis.Gameplay.Components;
using Genesis.Gameplay.Components.UI;
using Genesis.Gameplay.Extensions;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Genesis.GameStates.Menus.Settings;

public class KeyBindingsState : IGameState
{
    private readonly World mUiWorld = World.Create();
    
    private GameStateManager mGameStateManager;
    private GameServices mServices;
    private ScreenService mScreenService;

    private AudioService mAudioService;

    private List<Entity> mButtonList = [];
    
    private InputAction? mKeytoChange = null;

    private int Scrollamount = 0;
    

    public void Initialize(GameStateManager manager, GameServices services, ScreenService screen, AudioService sound)
    {
        mGameStateManager = manager;
        mServices = services;
        mScreenService = screen;
        mAudioService = sound;
    }

    public void Enter() => BuildUi(mUiWorld);
    public void Exit()
    {
        mUiWorld.Dispose();
    }

    public void Pause() {}
    public void Resume() {}

    public void HandleInput(InputService input)
    {
        if (input.IsActionPressed(InputAction.Pause))
        {
            mGameStateManager.PopState();
            return;
        }

        int scroll = input.GetMouseScroll();
        if(scroll> 0){ScrollUp(scroll);}
        else {ScrollDown(-scroll);}
        
        if (input.IsActionDown(InputAction.ScrollUp)){ScrollUp(4);}
        if (input.IsActionDown(InputAction.ScrollDown)){ScrollDown(4);}


        if (mKeytoChange != null)
        {
            
            var newKey = mServices.InputService.GetPressedKey();
            if (newKey != Keys.None)
            {
                mServices.InputService.ChangeKeyBinding((InputAction)mKeytoChange, newKey);
                mKeytoChange = null;
                BuildUi(mUiWorld, Scrollamount);
            }
        }
        
        
        mServices.Systems.HandleInput(mUiWorld, input);
    }

    public void Update(GameTime gameTime) => mServices.Systems.Update(mUiWorld, gameTime);

    public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        var uiScale = mScreenService.GetUiScale();
        spriteBatch.Begin(
            samplerState: SamplerState.PointClamp,
            transformMatrix: Matrix.CreateScale(uiScale, uiScale, 1.0f),
            sortMode: SpriteSortMode.FrontToBack
        );
        mServices.Systems.Draw(mUiWorld, spriteBatch);
        spriteBatch.End();
    }

    private void BuildUi(World world, int scrollPosition = 0)
    {
        mButtonList.Clear();
        world.Clear();
        const int nodeCount = 7;
        const float virtualWidth = ScreenService.VirtualWidth;
        const float virtualHeight = ScreenService.VirtualHeight;
        const float nodeGap = virtualHeight / 8f;
        
        const float startPositionX = virtualWidth / 2f;
        const float startPositionY = (virtualHeight - (nodeCount - 1) * nodeGap) / 2f;
        const int nodeWidth = (int)virtualWidth / 6;
        const int nodeHeight = (int)virtualHeight / 20;
        const int paddingX = (int)virtualWidth / 80;
        const int paddingY = (int)virtualHeight / 80;

        const float nodeXGap = nodeWidth  * 2;
        
        var targetPixels = new Rectangle(0, 0, nodeWidth, nodeHeight);
        var padding = new Point(paddingX, paddingY);
        
        var textFont = mServices.Content.Load<SpriteFont>("Fonts/HudFont");

        var keydict = mServices.InputService.GetKeyBindings();
        var keynames = mServices.InputService.GetKeyBindingNames();
        
        //gap of buttons
        int y = 0;
        
        //list of bindingbuttons
       
        foreach (var x in keydict)
        {
            //values of keybinding to string
            string str = "";
            foreach (var val in x.Value)
            {
                str = str + Enum.GetName(val) + ", ";
            }

            if (str.EndsWith(", ")) {str = str.Remove(str.Length - 2);}

            string bindingname = Enum.GetName(x.Key);
            if (keynames.ContainsKey(x.Key))
            {
                bindingname = keynames[x.Key];
            }

            var button =  mServices.UiFactory.CreateButtonWithSprite(
                world: world,
                position: new Vector2(startPositionX, startPositionY + y * nodeGap + Scrollamount),
                text: $"{bindingname}: {str}",
                onClick: () => { mKeytoChange = x.Key; },
                targetPixels: targetPixels,
                padding: padding + new Point($"{bindingname}: {str}".Length * 2,0)
            );
            
            mButtonList.Add(button);
            y++;
        }
        
        mServices.UiFactory.CreateButtonWithSprite(
            world: world,
            position: new Vector2(startPositionX + nodeXGap, startPositionY + nodeGap),
            text: "Scroll Up",
            onClick: () => ScrollUp(paddingY),
            targetPixels: targetPixels,
            padding: padding
        );
        
        mServices.UiFactory.CreateButtonWithSprite(
            world: world,
            position: new Vector2(startPositionX + nodeXGap, startPositionY + 2 * nodeGap),
            text: "Scroll Down",
            onClick: () => ScrollDown(paddingY),
            targetPixels: targetPixels,
            padding: padding
        );
        

            mServices.UiFactory.CreateButtonWithSprite(
            world: world,
            position: new Vector2(startPositionX + nodeXGap, startPositionY + 3 * nodeGap),
            text: "Return",
            onClick: () => mGameStateManager.PopState(),
            targetPixels: targetPixels,
            padding: padding
        );
            
        mServices.UiFactory.CreateButtonWithSprite(
            world: world,
            position: new Vector2(startPositionX + nodeXGap, startPositionY + 4 * nodeGap),
            text: "Default",
            onClick: () => {mServices.InputService.ChangeKeyBindingDefault();
                BuildUi(mUiWorld,scrollPosition);},
            targetPixels: targetPixels,
            padding: padding
        );

    }

    private void ScrollUp(int amount)
    {
        if (Scrollamount > 10) return;
        foreach(var button in mButtonList)
        {
            var temp =  mUiWorld.Get<PositionComponent>(button).Value;
            temp.Y += amount;
            mUiWorld.Get<PositionComponent>(button).Value = temp;
            
            
            

        }
        Scrollamount += amount;
        
    }
    private void ScrollDown(int amount)
    {
        if (Scrollamount < -ScreenService.VirtualHeight * 2) return;
        foreach(var button in mButtonList)
        {
            var temp =  mUiWorld.Get<PositionComponent>(button).Value;
            temp.Y -= amount;
            mUiWorld.Get<PositionComponent>(button).Value = temp;
            



        }
        Scrollamount -= amount;
    }
    private void ClearButtons(World world)
    {
        foreach (var button in mButtonList)
        {
            world.Destroy(button);
        }
    }

    private void ChangeButtonText(string text, Entity entity)
    {
        
    }
    
}