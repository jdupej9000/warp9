using System;

namespace Warp9.Viewer
{
    public static class StockShaders
    {
        public static readonly ShaderSpec VsSimple = new ShaderSpec(
            kind: ShaderKind.Vertex,
            input: [
                (ShaderDataType.Float3, "pos"),
                (ShaderDataType.Float3, "normal")
            ],
            output: [
                (ShaderDataType.Float3, "frag_pos"),
                (ShaderDataType.Float3, "frag_normal")
            ],
            uniform: [
                (ShaderDataType.Float4x4, "viewproj")
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
                (ShaderDataType.Float3, "frag_pos"),
                (ShaderDataType.Float3, "frag_normal")
            ],
            output: [
                (ShaderDataType.Float4, "frag_color")
            ],
            uniform: [
                (ShaderDataType.Float3, "light_pos"),
                (ShaderDataType.Float3, "camera_pos"),
                (ShaderDataType.Int, "flags"),
                (ShaderDataType.Float4, "color"),
            ],
            code: """
            void main() {
                vec3 ret = vec3(0,0,0);
                vec3 col = color;
                
                if((mode & 0x3u) == 0x1u) {
                    //col = frag_col;
                }
                else if((mode & 0x3u) == 0x2u) {
                    //col = texture(texture_color, frag_tex0).rgba;
                }
                else if((mode & 0x3u) == 0x3u) {
                    //col = vec4(color.rgb * frag_value, 1);
                    //col = texture(lut_color, frag_value).rgba;
                }

                if((mode & 0x40u) == 0x40u) {
                    vec3 ddx = dFdx(frag_pos);
                    vec3 ddy = dFdy(frag_pos);
                    n = normalize(cross(ddx, ddy));
                }
            
                if((mode & 0xcu) == 0x0u) {
                    ret = col.xyz;
                }
                else {
                    vec3 amb = 0.1 * col.xyz;            
                    vec3 light_dir = normalize(light_pos - frag_pos);
                    float diff = abs(dot(n, light_dir));
                    vec3 dif = diff * col.xyz;
                    ret = (amb + dif);

                    if((mode & 0xcu) == 0x4u) {
                        // TODO: specular
                    }
                }
            
                frag_color = vec4(ret, col.w);
            }
            """
        );
    }
}