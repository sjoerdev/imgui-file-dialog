## Description:
This is a file dialog imgui implementation for C# and modern dotnet.

## Example Image:
![filedialog](https://github.com/user-attachments/assets/06ad8c66-140d-4d14-92cd-41ed229988ff)

## Simple Usage:
```csharp
ImGui.Begin("Window");
ImGui.SetWindowSize(new Vector2(600, 300), ImGuiCond.Once);
if (ImGui.Button("Open file dialog")) fileDialogOpen = true;
if (!fileDialogOpen && !string.IsNullOrEmpty(selectedPath)) ImGui.Text($"Selected path: {selectedPath}");
else ImGui.Text("Selected path: ?");
if (fileDialogOpen) FileDialog.Show(ref fileDialogOpen, ref selectedPath, FileDialog.DialogType.OpenFile);
ImGui.End();
```

## Building Example:

Download .NET 9: https://dotnet.microsoft.com/en-us/download

Building for Windows: ``dotnet publish -o ./build/windows --sc true -r win-x64 -c release``

Building for Linux: ``dotnet publish -o ./build/linux --sc true -r linux-x64 -c release``
