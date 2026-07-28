using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Arch.Core;
using Arch.Core.Extensions;
using ImGuiNET;

namespace Genesis.Architecture.Debug;

public class EntityInspector(World world)
{
    private Entity mSelectedEntity = Entity.Null;
    private readonly List<Entity> mEntityBuffer = new(1024);

    private const ImGuiTableFlags TableFlags =
        ImGuiTableFlags.Borders | ImGuiTableFlags.Resizable | ImGuiTableFlags.RowBg;
    public void Draw()
    {
        ImGui.Begin("Entity Inspector");

        if (ImGui.BeginTable("InspectorTable", 2, TableFlags))
        {
            // Column Setup
            ImGui.TableSetupColumn("Entities", ImGuiTableColumnFlags.WidthFixed, 150f);
            ImGui.TableSetupColumn("Components", ImGuiTableColumnFlags.WidthStretch, 100f);
            ImGui.TableHeadersRow();
            ImGui.TableNextRow();

            // Write Entity List to left column
            ImGui.TableSetColumnIndex(0);
            DrawEntityList();
            
            // Write Component Inspector to right column
            ImGui.TableSetColumnIndex(1);
            if (mSelectedEntity != Entity.Null && world.IsAlive(mSelectedEntity)) {DrawComponents(mSelectedEntity);}
            else {ImGui.TextDisabled("Select an entity to view details.");}
            
            ImGui.EndTable();
        }
        
        ImGui.End();
    }

    private void DrawEntityList()
    {
        ImGui.Text("Entities");
        ImGui.Separator();
        
        // Capture & Sort entities in a buffer
        mEntityBuffer.Clear();
        var query = new QueryDescription();
        world.Query(query, entity => mEntityBuffer.Add(entity));
        mEntityBuffer.Sort((a, b) => a.Id.CompareTo(b.Id));

        // Draw the ImGui List
        foreach (var entity in mEntityBuffer)
        {
            var label = $$"""Entity {Id = {{entity.Id}}}""";
            var isSelected = mSelectedEntity == entity;
            if (ImGui.Selectable(label, isSelected)) {mSelectedEntity = entity;}
        }
        
    }

    private void DrawComponents(Entity entity)
    {
        ImGui.Text($"Selected: ID {mSelectedEntity.Id} | Version {mSelectedEntity.Version}");
        ImGui.Text($"IsAlive: {world.IsAlive(mSelectedEntity)}");

        if (entity == Entity.Null || !world.IsAlive(entity))
        {
            return;
            
        }
        
        ImGui.Text($"ID: {entity.Id}");
        ImGui.Separator();

        var components = entity.GetAllComponents();
        foreach (var component in components)
        {
            if (component is null) {return;}
            var type = component.GetType();
            
            ImGui.PushID(type.Name);
            var isOpen = ImGui.CollapsingHeader(type.Name, ImGuiTreeNodeFlags.DefaultOpen);
            if (isOpen)
            {
                var changed = DrawComponentFields(component, type);
                if (changed) {SetComponentDynamic(entity, component, type);}
            }
            
            ImGui.PopID();
        }
    }

    private bool DrawComponentFields(object component, Type type)
    {
        var anyChanged = false;
        
        foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
        {
            var value = field.GetValue(component);
            var changed = DrawWidget(field.Name, ref value);
            if (!changed) {continue;}
            
            field.SetValue(component, value);
            anyChanged = true;
        }

        foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!property.CanRead || !property.CanWrite) {continue;}
            if (property.GetIndexParameters().Length > 0) {continue;}
            
            var value = property.GetValue(component);
            var changed = DrawWidget(property.Name, ref value);
            if (!changed) {continue;}
            
            property.SetValue(component, value);
            anyChanged = true;
        }
        
        return anyChanged;
    }

    private bool DrawWidget(string name, ref object value)
    {
        var changed = false;
        switch (value)
        {
            case int iVal:
                if (ImGui.DragInt(name, ref iVal))
                {
                    value = iVal;
                    changed = true;
                }
                break;
            
            case float fVal:
                if (ImGui.DragFloat(name, ref fVal))
                {
                    value = fVal;
                    changed = true;
                }
                break;
            
            case bool bVal:
                if (ImGui.Checkbox(name, ref bVal))
                {
                    value = bVal;
                    changed = true;
                }
                break;
            
            case string strVal:
                if (ImGui.InputText(name, ref strVal, 100))
                {
                    value = strVal;
                    changed = true;
                }
                break;
            
            case Microsoft.Xna.Framework.Vector2 vec2Val:
                System.Numerics.Vector2 vec2 = new(vec2Val.X, vec2Val.Y);
                if (ImGui.DragFloat2(name, ref vec2))
                {
                    value = new Microsoft.Xna.Framework.Vector2(vec2Val.X, vec2Val.Y);
                    changed = true;
                }
                break;
            
            case Microsoft.Xna.Framework.Color colorVal:
                var vec4 = colorVal.ToVector4().ToNumerics();
                if (ImGui.DragFloat4(name, ref vec4))
                {
                    value = new Microsoft.Xna.Framework.Color(vec4.X, vec4.Y, vec4.Z, vec4.W);
                    changed = true;
                }
                break;
            
            case Enum enumVal:
                var type = enumVal.GetType();
                var names = Enum.GetNames(type);
                var index = Array.IndexOf(names, enumVal.ToString());

                if (ImGui.Combo(name, ref index, names, names.Length))
                {
                    value = Enum.Parse(type, names[index]);
                    changed = true;
                }
                break;
        }
        
        return changed;
    }

    private void SetComponentDynamic(Entity entity, object component, Type type)
    {
        var methodDefinition = typeof(World).GetMethods().FirstOrDefault(m =>
            m.Name == "Set" &&
            m.IsGenericMethod &&
            m.GetGenericArguments().Length == 1 &&
            m.GetParameters().Length == 2 &&
            m.GetParameters()[0].ParameterType == typeof(Entity)
        );

        if (methodDefinition is not null)
        {
            var specificMethod = methodDefinition.MakeGenericMethod(type);
            specificMethod.Invoke(world, [entity, component]);
        }
        else { throw new Exception("Entity has no Set method"); }
    }

}

public static class ImGuiExtensions
{
    public static System.Numerics.Vector4 ToNumerics(this Microsoft.Xna.Framework.Vector4 v) => new(v.X, v.Y, v.Z, v.W);
    public static Microsoft.Xna.Framework.Vector4 ToXna(this System.Numerics.Vector4 v) => new(v.X, v.Y, v.Z, v.W);
}
