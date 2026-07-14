using System;

namespace Warp9.Viewer
{
    public static class StockShaders
    {
        public static readonly ShaderSpec VsSimple = new ShaderSpec(
            kind: ShaderKind.Vertex,
            input: [
                (DataType.Float3, "pos"),
                (DataType.Float3, "normal")
            ],
            output: [
                (DataType.Float3, "frag_pos"),
                (DataType.Float3, "frag_normal")
            ],
            uniform: [
                (DataType.Float4x4, "viewproj")
            ],
            code: """
                void main() {
                    frag_pos = pos;
                    frag_normal = normal;
                    gl_Position = viewproj * vec4(pos, 1.0);
                }
            """
        );

        public static readonly ShaderSpec PsSimple = new ShaderSpec(
            kind: ShaderKind.Pixel,
            input: [
                (DataType.Float3, "frag_pos"),
                (DataType.Float3, "frag_normal")
            ],
            output: [
                (DataType.Float4, "frag_color")
            ],
            uniform: [
                (DataType.Float3, "light_pos"),
                (DataType.Float3, "camera_pos"),
                (DataType.Int, "flags"),
                (DataType.Float4, "color"),
            ],
            code: """
            void main() {
                vec3 ret = vec3(0,0,0);
                vec3 n = frag_normal;
                vec4 col = color;

                if((flags & 0x3) == 0x1) {
                    //col = frag_col;
                }
                else if((flags & 0x3) == 0x2) {
                    //col = texture(texture_color, frag_tex0).rgba;
                }
                else if((flags & 0x3) == 0x3) {
                    //col = vec4(color.rgb * frag_value, 1);
                    //col = texture(lut_color, frag_value).rgba;
                }

                if((flags & 0x40) == 0x40) {
                    vec3 ddx = dFdx(frag_pos);
                    vec3 ddy = dFdy(frag_pos);
                    n = normalize(cross(ddx, ddy));
                }
            
                if((flags & 0xc) == 0x0) {
                    ret = col.xyz;
                }
                else {
                    vec3 amb = 0.1 * col.xyz;            
                    vec3 light_dir = normalize(light_pos - frag_pos);
                    float diff = abs(dot(n, light_dir));
                    vec3 dif = diff * col.xyz;
                    ret = (amb + dif);

                    if((flags & 0xc) == 0x4) {
                        // TODO: specular
                    }
                }
            
                frag_color = vec4(ret, col.w);
            }
            """
        );
    }
}