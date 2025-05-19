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
        if (ImGui.Begin(title, ImGuiWindowFlags.NoResize))
        {
            // collect directory entries
            var dir = new DirectoryInfo(currentPath);
            var files = dir.GetFiles().ToList();
            var folders = dir.GetDirectories().ToList();

            // display current path
            ImGui.Text(currentPath);

            // folder panel
            ImGui.BeginChild("Folders", new Vector2(200, 300), ImGuiWindowFlags.HorizontalScrollbar);

            if (ImGui.Selectable("..", false, ImGuiSelectableFlags.AllowDoubleClick))
            {
                if (ImGui.IsMouseDoubleClicked(0))
                {
                    var parent = Directory.GetParent(currentPath);
                    if (parent != null) currentPath = parent.FullName;
                }
            }

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

            // File panel
            ImGui.BeginChild("Files", new Vector2(516, 300), ImGuiWindowFlags.HorizontalScrollbar);
            ImGui.Columns(4);

            if (ImGui.Selectable("File"))
                ToggleSort(ref fileNameSortOrder, ref sizeSortOrder, ref dateSortOrder, ref typeSortOrder);
            ImGui.NextColumn();

            if (ImGui.Selectable("Size"))
                ToggleSort(ref sizeSortOrder, ref fileNameSortOrder, ref dateSortOrder, ref typeSortOrder);
            ImGui.NextColumn();

            if (ImGui.Selectable("Type"))
                ToggleSort(ref typeSortOrder, ref fileNameSortOrder, ref sizeSortOrder, ref dateSortOrder);
            ImGui.NextColumn();

            if (ImGui.Selectable("Date"))
                ToggleSort(ref dateSortOrder, ref fileNameSortOrder, ref sizeSortOrder, ref typeSortOrder);
            ImGui.NextColumn();

            ImGui.Separator();

            SortFiles(ref files);

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

            // Display selected path
            string selectedPath = Path.Combine(currentPath, !string.IsNullOrEmpty(currentFolder) ? currentFolder : currentFile);
            ImGui.PushItemWidth(724);
            ImGui.InputText("##selected", ref selectedPath, 512, ImGuiInputTextFlags.ReadOnly);

            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 6);

            // Folder operations
            if (ImGui.Button("New folder"))
                ImGui.OpenPopup("NewFolderPopup");

            ImGui.SameLine();
            bool canDelete = !string.IsNullOrEmpty(currentFolder);

            if (!canDelete)
            {
                ImGui.BeginDisabled();
            }

            if (ImGui.Button("Delete folder"))
                ImGui.OpenPopup("DeleteFolderPopup");

            if (!canDelete)
            {
                ImGui.EndDisabled();
            }

            // New folder popup
            if (ImGui.BeginPopupModal("NewFolderPopup"))
            {
                ImGui.Text("Enter folder name");
                ImGui.InputText("##foldername", ref newFolderName, 100);

                if (ImGui.Button("Create"))
                {
                    if (string.IsNullOrWhiteSpace(newFolderName))
                    {
                        newFolderError = "Name cannot be empty";
                    }
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
                if (ImGui.Button("Cancel"))
                {
                    newFolderName = string.Empty;
                    newFolderError = string.Empty;
                    ImGui.CloseCurrentPopup();
                }

                if (!string.IsNullOrEmpty(newFolderError))
                {
                    ImGui.TextColored(new Vector4(1, 0, 0, 1), newFolderError);
                }

                ImGui.EndPopup();
            }

            // Delete folder popup
            if (ImGui.BeginPopupModal("DeleteFolderPopup"))
            {
                ImGui.TextColored(new Vector4(1, 0, 0, 1), $"Are you sure you want to delete '{currentFolder}'?");
                if (ImGui.Button("Yes"))
                {
                    Directory.Delete(Path.Combine(currentPath, currentFolder), true);
                    currentFolder = string.Empty;
                    ImGui.CloseCurrentPopup();
                }

                ImGui.SameLine();
                if (ImGui.Button("No"))
                {
                    ImGui.CloseCurrentPopup();
                }

                ImGui.EndPopup();
            }

            ImGui.SameLine();
            ImGui.SetCursorPosX(ImGui.GetWindowWidth() - 120);

            if (ImGui.Button("Cancel"))
            {
                Reset();
                open = false;
            }

            ImGui.SameLine();
            if (ImGui.Button("Choose"))
            {
                if (type == DialogType.SelectFolder && string.IsNullOrEmpty(currentFolder))
                {
                    fileDialogError = "You must select a folder!";
                }
                else if (type == DialogType.OpenFile && string.IsNullOrEmpty(currentFile))
                {
                    fileDialogError = "You must select a file!";
                }
                else
                {
                    resultPath = Path.Combine(currentPath, !string.IsNullOrEmpty(currentFolder) ? currentFolder : currentFile);
                    fileDialogError = string.Empty;
                    Reset();
                    open = false;
                }
            }

            if (!string.IsNullOrEmpty(fileDialogError))
            {
                ImGui.TextColored(new Vector4(1, 0, 0, 1), fileDialogError);
            }

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