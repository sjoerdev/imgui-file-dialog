using System.IO;
using System.Linq;
using System.Numerics;
using System.Collections.Generic;

using Hexa.NET.ImGui;

namespace Project;

public static class FileDialog
{
    static bool initialPathSet = false;

    static int fileSelectIndex = 0;
    static int folderSelectIndex = 0;

    static string currentPath = Directory.GetCurrentDirectory();
    static string currentFile = string.Empty;
    static string currentFolder = string.Empty;

    static string fileDialogError = string.Empty;

    static SortOrder fileNameSortOrder = SortOrder.None;
    static SortOrder sizeSortOrder = SortOrder.None;
    static SortOrder dateSortOrder = SortOrder.None;
    static SortOrder typeSortOrder = SortOrder.None;

    static string newFolderName = string.Empty;
    static string newFolderError = string.Empty;

    public static void Show(ref bool open, ref string resultPath, DialogType type = DialogType.OpenFile)
    {
        // return if it shouldnt be open
        if (!open) return;

        // try to set initial path
        if (!initialPathSet && !string.IsNullOrEmpty(resultPath))
        {
            if (Directory.Exists(resultPath)) currentPath = resultPath;
            else if (File.Exists(resultPath)) currentPath = Path.GetDirectoryName(resultPath);
            else currentPath = Directory.GetCurrentDirectory();
            initialPathSet = true;
        }

        // setup the imgui window
        string title = type == DialogType.OpenFile ? "Select a file" : "Select a folder";
        ImGui.SetNextWindowSize(new Vector2(740, 410), ImGuiCond.FirstUseEver);
        if (ImGui.Begin(title, ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse))
        {
            // read the directory
            var directory = new DirectoryInfo(currentPath);
            var files = directory.GetFiles().ToList();
            var folders = directory.GetDirectories().ToList();

            // display current path
            ImGui.Text(currentPath);

            // folder panel
            ImGui.BeginChild("Folders", new Vector2(200, 300), ImGuiWindowFlags.HorizontalScrollbar);

            // button to go up a directory
            if (ImGui.Selectable("..", false, ImGuiSelectableFlags.AllowDoubleClick))
            {
                if (ImGui.IsMouseDoubleClicked(0))
                {
                    var parent = Directory.GetParent(currentPath);
                    if (parent != null) currentPath = parent.FullName;
                }
            }

            // list folders as selectable buttons
            for (int i = 0; i < folders.Count; i++)
            {
                bool selected = i == folderSelectIndex;
                if (ImGui.Selectable(folders[i].Name, selected, ImGuiSelectableFlags.AllowDoubleClick))
                {
                    currentFile = string.Empty;
                    if (ImGui.IsMouseDoubleClicked(0))
                    {
                        currentPath = folders[i].FullName;
                        folderSelectIndex = 0;
                        fileSelectIndex = 0;
                        currentFolder = string.Empty;
                    }
                    else
                    {
                        folderSelectIndex = i;
                        currentFolder = folders[i].Name;
                    }
                }
            }

            ImGui.EndChild();
            ImGui.SameLine();

            // file panel
            ImGui.BeginChild("Files", new Vector2(516, 300), ImGuiWindowFlags.HorizontalScrollbar);

            // row of sorting buttons
            ImGui.Columns(4);
            if (ImGui.Selectable("File")) ToggleSort(ref fileNameSortOrder, ref sizeSortOrder, ref dateSortOrder, ref typeSortOrder);
            ImGui.NextColumn();
            if (ImGui.Selectable("Size")) ToggleSort(ref sizeSortOrder, ref fileNameSortOrder, ref dateSortOrder, ref typeSortOrder);
            ImGui.NextColumn();
            if (ImGui.Selectable("Type")) ToggleSort(ref typeSortOrder, ref fileNameSortOrder, ref sizeSortOrder, ref dateSortOrder);
            ImGui.NextColumn();
            if (ImGui.Selectable("Date")) ToggleSort(ref dateSortOrder, ref fileNameSortOrder, ref sizeSortOrder, ref typeSortOrder);
            ImGui.NextColumn();
            ImGui.Separator();
            SortFiles(ref files);

            // list of files as buttons
            for (int i = 0; i < files.Count; i++)
            {
                var file = files[i];
                bool selected = i == fileSelectIndex;
                if (ImGui.Selectable(file.Name, selected, ImGuiSelectableFlags.AllowDoubleClick))
                {
                    fileSelectIndex = i;
                    currentFile = file.Name;
                    currentFolder = string.Empty;
                }

                ImGui.NextColumn();
                ImGui.Text(file.Length.ToString());
                ImGui.NextColumn();
                ImGui.Text(file.Extension);
                ImGui.NextColumn();
                ImGui.Text(file.LastWriteTime.ToString("yyyy-MM-dd HH:mm"));
                ImGui.NextColumn();
            }

            ImGui.EndChild();

            // display selected path
            string selectedPath = Path.Combine(currentPath, !string.IsNullOrEmpty(currentFolder) ? currentFolder : currentFile);
            ImGui.PushItemWidth(724);
            ImGui.InputText("##selected", ref selectedPath, 512, ImGuiInputTextFlags.ReadOnly);

            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 6);

            // new folder button
            if (ImGui.Button("New folder")) ImGui.OpenPopup("Create Folder");

            ImGui.SameLine();

            // delete folder button
            bool canDelete = !string.IsNullOrEmpty(currentFolder);
            if (!canDelete) ImGui.BeginDisabled();
            if (ImGui.Button("Delete folder")) ImGui.OpenPopup("Delete Folder");
            if (!canDelete) ImGui.EndDisabled();

            // new folder popup
            if (ImGui.BeginPopupModal("Create Folder", ImGuiWindowFlags.NoResize))
            {
                ImGui.Text("Name:");
                ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                ImGui.InputText("##foldername", ref newFolderName, 100);

                // create button
                if (ImGui.Button("Create"))
                {
                    if (string.IsNullOrWhiteSpace(newFolderName)) newFolderError = "Name cannot be empty";
                    else
                    {
                        var path = Path.Combine(currentPath, newFolderName);
                        Directory.CreateDirectory(path);
                        newFolderName = string.Empty;
                        newFolderError = string.Empty;
                        ImGui.CloseCurrentPopup();
                    }
                }

                ImGui.SameLine();

                // cancel button
                if (ImGui.Button("Cancel"))
                {
                    newFolderName = string.Empty;
                    newFolderError = string.Empty;
                    ImGui.CloseCurrentPopup();
                }

                // deal with folder errors
                if (!string.IsNullOrEmpty(newFolderError)) ImGui.TextColored(new Vector4(1, 0, 0, 1), newFolderError);

                ImGui.EndPopup();
            }

            // delete folder popup
            if (ImGui.BeginPopupModal("Delete Folder", ImGuiWindowFlags.NoResize))
            {
                ImGui.TextColored(new Vector4(1, 0, 0, 1), $"Are you sure you want to delete '{currentFolder}'?");
                ImGui.Spacing();

                var width = ImGui.GetContentRegionAvail().X / 2 - (ImGui.GetStyle().ItemSpacing.X / 2);
                if (ImGui.Button("Yes", new(width, 0)))
                {
                    Directory.Delete(Path.Combine(currentPath, currentFolder), true);
                    currentFolder = string.Empty;
                    ImGui.CloseCurrentPopup();
                }

                ImGui.SameLine();

                if (ImGui.Button("No", new(width, 0)))
                {
                    ImGui.CloseCurrentPopup();
                }

                ImGui.EndPopup();
            }

            ImGui.SameLine();
            ImGui.SetCursorPosX(ImGui.GetWindowWidth() - 120);

            // cancel dialog button
            if (ImGui.Button("Cancel"))
            {
                Reset();
                open = false;
            }

            ImGui.SameLine();

            // choose selected file or folder button
            if (ImGui.Button("Choose"))
            {
                if (type == DialogType.SelectFolder && string.IsNullOrEmpty(currentFolder)) fileDialogError = "You must select a folder!";
                else if (type == DialogType.OpenFile && string.IsNullOrEmpty(currentFile)) fileDialogError = "You must select a file!";
                else
                {
                    resultPath = Path.Combine(currentPath, !string.IsNullOrEmpty(currentFolder) ? currentFolder : currentFile);
                    fileDialogError = string.Empty;
                    Reset();
                    open = false;
                }
            }

            // show file dialog error
            if (!string.IsNullOrEmpty(fileDialogError)) ImGui.TextColored(new Vector4(1, 0, 0, 1), fileDialogError);

            ImGui.End();
        }
    }

    static void Reset()
    {
        fileSelectIndex = 0;
        folderSelectIndex = 0;
        currentFile = string.Empty;
        currentFolder = string.Empty;
        fileDialogError = string.Empty;
        newFolderError = string.Empty;
        initialPathSet = false;
    }

    static void ToggleSort(ref SortOrder target, ref SortOrder a, ref SortOrder b, ref SortOrder c)
    {
        a = b = c = SortOrder.None;
        target = target == SortOrder.Down ? SortOrder.Up : SortOrder.Down;
    }

    static void SortFiles(ref List<FileInfo> files)
    {
        if (fileNameSortOrder != SortOrder.None)
        {
            files = files.OrderBy(f => f.Name).ToList();
            if (fileNameSortOrder == SortOrder.Down) files.Reverse();
        }
        else if (sizeSortOrder != SortOrder.None)
        {
            files = files.OrderBy(f => f.Length).ToList();
            if (sizeSortOrder == SortOrder.Down) files.Reverse();
        }
        else if (typeSortOrder != SortOrder.None)
        {
            files = files.OrderBy(f => f.Extension).ToList();
            if (typeSortOrder == SortOrder.Down) files.Reverse();
        }
        else if (dateSortOrder != SortOrder.None)
        {
            files = files.OrderBy(f => f.LastWriteTime).ToList();
            if (dateSortOrder == SortOrder.Down) files.Reverse();
        }
    }

    public enum DialogType
    {
        OpenFile,
        SelectFolder
    }

    public enum SortOrder
    {
        Up,
        Down,
        None
    }
}