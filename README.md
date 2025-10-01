# warp9

> [!WARNING]
> Warp9 is very much work in progress. Proceed with caution. There might also be dragons.


This is a toolkit primarily intended for dense geometric morphometry.
The toolkit can be broken down to these parts:
- **WarpCore** is a native library that concentrates all the performance-intensive code. 
Its features include registration algorithms, spatial searching structures, data conversion etc.
- **WarpViewer** is a .NET library that provides 3D rendering capabilities over Direct3D 11, as well as data structures for holding meshes and similar data.
- **WarpProcessing** has native imports from WarpCore and uses them to perform mesh processing, DCA, Procrustes transform etc.
- **Warp9** is finally the application that users can interact with. Written over WPF with a customizable theme.

## Build instructions
Ensure you have the following dependencies installed:
- Visual Studio 2022
- CUDA Toolkit 12.9 with VS integration

**1. Install OpenBLAS**<br>
Use the script `tools\Install-Deps.ps1` to download and install OpenBLAS. You should see it appear in the `thirdparty` directory.

**2. Build warp9**<br>
Open `Warp9.sln` in Visual Studio and build it.

**3. Run unit tests (optional)**<br>
Use Visual Studio's Test explorer to find and execute unit tests. 
Certain tests will not run and will report as inconclusive because they depend on data files that are not public.
If replacement data become available in public domain, we are open to using those instead.
Some unit tests generate bitmaps, these can be found in `bin\testresults`.