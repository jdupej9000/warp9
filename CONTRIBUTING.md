You can contribute to warp9 with pull requests or issues. Any contributions are much appreciated.

When adding test data to unit tests, make sure that the data can be used in public domain.

Please adhere to the coding guidelines below.

## C/C++ and CUDA
### Code style 
We use K&R brace style, with `else` on the same line as the previous closing brace (like 1TBS).
```cpp
if(condition) {
} else {
}
```

If the body of a conditional expression is a one-liner and there is no `else` or `else if` block,
it is all right to omit braces.

```cpp
if(condition)   // OK
   return;
```

Pointers have the asterisk attached to the type, not variable name.
```cpp
char* pbuffer;          // OK
int *wtf;               // sooo not OK
float * so_much_air;    // also NO
```

Use post-increment whenever either is OK logic-wise.
```cpp
for(int i = 0; i < n; i++) // OK
for(int i = 0; i < n; ++i) // NOT OK
```

Do not use the alternate function declaration syntax unless unavoidable.
```cpp
auto f(int a, int b) -> int; // NO!
```

If a function has no arguments, use `void`:
```cpp
void action(void); // this is the way
```

Feel free to include blank lines whenever it is useful for readability.

### Performance
AVX2+FMA3 is the minimum required ISA. All code written for AVX512 or CUDA must have an AVX2 or unoptimized path.

Avoid exceptions in performance-intensive code.

## C#
### Code style
Allman-style braces are used.
```cs
if(condition)
{
}
else
{
}
```

State concrete types when declaring variables rather than using `var`, unless the type name is extremely large.
```cs
int x = 5; // OK
var y = 4.1; // NO, typing 'double' is not too much work
var z = new Dictionary<string, LocaleAgnosticParametricTextToDoubleOrFloatConverter<Something, SomethingElse, double>>(); // here it is OK
```

Avoid range notations.
```cs
ReadOnlySpan<byte> x = GetSomething();
ReadOnlySpan<byte> y = x.Slice(10,20);  // OK
ReadOnlySpan<byte> z = y[10..20];       // NO!
```

Avoid typeless `new()`.
```cs
List<string> x = new List<string>();    // OK
List<string> y = new();                 // NO
```

Avoid local methods.

Nullable references should be enabled.

### Performance
Avoid using `async` constructs, unless close to the GUI.

Exceptions are used only to indicate contract violations (e.g. empty string given to a method while non-empty is required) and usually tear down the entire application or a large chunk of it. Exceptions are not to be used for flow control (e.g. indicate server refusing connection) despite this tendency being prevalent in .NET. Do not use exceptions at all if the code is performance intensive.
