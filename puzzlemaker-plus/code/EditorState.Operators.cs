using System;
using Godot;
using PuzzlemakerPlus.Commands;
using PuzzlemakerPlus.Items;

namespace PuzzlemakerPlus;

// Various operators that can be called from GDScript
public partial class EditorState
{
    public PuzzlemakerProject NewProject()
    {
        GD.Print("Creating new project...");
        PuzzlemakerProject project = new PuzzlemakerProject();
        OpenProject(project);
        return project;
    }

    public void OpenProject(PuzzlemakerProject project)
    {
        if (project == Project) return;
        Project = project;
        SetSelection(default);
        SetSelectedItems(Array.Empty<Item>());
        EmitSignal(SignalName.OnOpenProject, project);
        EmitSignal(SignalName.OnChangeProjectName, project.FileName!);
        UnSaved = false;
    }

    public void OpenProjectFile(string filepath)
    {
        PuzzlemakerProject? project;
        using (var stream = new FileAccessStream(filepath))
        {
            project = PuzzlemakerProject.ReadFile(stream);
        }
        if (project == null)
            throw new Exception("Json deserializer returned null.");
        project.FileName = filepath;
        OpenProject(project);
    }

    public void SaveProjectAs(string filepath)
    {
        using (var stream = new FileAccessStream(filepath, Godot.FileAccess.ModeFlags.Write))
        {
            Project.WriteFile(stream);
        }
        ProjectName = filepath;
        UnSaved = false;
        GD.Print("Saved to " + filepath);
    }

    public void SaveProject()
    {
        if (!HasProjectName)
            throw new InvalidOperationException("Project name has not been set. Please use SaveProjectAs()");

        SaveProjectAs(ProjectName!);
    }

    public bool Undo()
    {
        return CommandStack.Undo();
    }

    public bool Redo()
    {
        return CommandStack.Redo();
    }

    public bool AddItem(string itemType, Vector3 position)
    {
        return CommandStack.Execute(new AddItemCommand(itemType, position));
    }

    public bool MoveItem(Item item, Vector3 position, Vector3 rotation)
    {
        return CommandStack.Execute(new MoveItemCommand(item, position, rotation));
    }
}
