# warp9

> [!WARNING]
> Warp9 is very much work in progress. Proceed with caution. There might also be dragons.


This is a toolkit primarily intended for dense geometric morphometry.
The toolkit can be broken down to these parts:
- **WarpCore** is a native library that concentrates all the performance-intensive code. 
Its features include registration algorithms, spatial searching structures, data conversion etc.
- **WarpViewer** is a .NET library that provides 3D rendering capabilities over Direct3D 11, as well as data structures for holding meshes and similar data.
- **WarpProcessing** has native imports from WarpCore and uses them to perform mesh processing, DCA, Procrustes transform etc.
- **Warp9** is finally the application that users can interact with. Proudly old-school written over WinForms and .NET.

## Build instructions
Ensure you have the following dependencies installed:
- Intel oneAPI with Visual Studio integration
