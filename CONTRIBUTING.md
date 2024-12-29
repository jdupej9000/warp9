You can contribute to warp9 with pull requests or issues. Any contributions are much appreciated.

## Contributing code
Please adhere to the coding guidelines below.

When adding test data to unit tests, make sure that the data can be used in public domain.

### C/C++
- We use K&R brace style.
- Pointers have the asterisk attached to the type; i.e. `char* pX`.
- Use `const` on function arguments where possible.
- AVX2 is the minimum required ISA. All code written for AVX512/AMX or CUDA must have an AVX2 or unoptimized path.
- Avoid exceptions in performance-intensive code.

### C#
- Allman-style braces are used.
- State concrete types when declaring variables rather than using `var`, unless the type name is extremely large.
- Avoid string interpolation, range notations and typeless `new()`.
- Nullable references should be enabled.
- Avoid exceptions in performance-intensive code.
