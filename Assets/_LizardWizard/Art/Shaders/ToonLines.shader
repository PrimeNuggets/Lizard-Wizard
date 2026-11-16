//-- soft screen space lines for small details (nostrils, cracks, creases) and gentle outlines on props
//-- runs from the Full Screen Pass renderer feature on PC_Renderer, needs depth + normals (see Toon_Credits.txt)
Shader "LizardWizard/ToonLines"
{
    Properties
    {
        _LineColor ("Line Color (multiplies the surface)", Color) = (0.38, 0.27, 0.25, 1)
        _LineWidth ("Line Width (pixels at 1080p)", Range(0.5, 3)) = 1
        _SilhouetteStrength ("Silhouette Strength", Range(0, 1)) = 0.3
        _CreaseStrength ("Crease Strength", Range(0, 1)) = 0.3
        _CreaseThreshold ("Crease Threshold", Range(0, 1)) = 0.25
        _DentStrength ("Dent Strength (nostrils, cracks)", Range(0, 1)) = 0.6
        _DentThreshold ("Dent Threshold", Range(0.0002, 0.02)) = 0.0007
        _DentRadius ("Dent Size (pixels at 1080p)", Range(1, 10)) = 5
        _FadeStart ("Fade Start", Float) = 30
        _FadeEnd ("Fade End", Float) = 60
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off
        ZTest Always
        Cull Off
        Blend Off

        Pass
        {
            Name "ToonLines"

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"

            half4 _LineColor;
            float _LineWidth;
            float _SilhouetteStrength;
            float _CreaseStrength;
            float _CreaseThreshold;
            float _DentStrength;
            float _DentThreshold;
            float _DentRadius;
            float _FadeStart;
            float _FadeEnd;

            float Depth(float2 uv)
            {
                return LinearEyeDepth(SampleSceneDepth(uv), _ZBufferParams);
            }

            //-- 1 / depth changes evenly across flat ground, so flat sand seen at a low angle
            //-- doesn't get mistaken for an edge or a dent
            float InvDepth(float2 uv)
            {
                return 1.0 / max(Depth(uv), 0.0001);
            }

            //-- how much deeper this pixel is than two opposite neighbours. if either one lands on a
            //-- different object the pair is skipped, otherwise the sand next to a character reads as a dent
            float DentPair(float2 uv, float2 offset, float id)
            {
                float a = InvDepth(uv + offset);
                float b = InvDepth(uv - offset);
                bool sameSurface = abs(a - id) < id * 0.03 && abs(b - id) < id * 0.03;
                return sameSurface ? (a + b) * 0.5 - id : 0;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv = input.texcoord;
                half3 color = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_PointClamp, uv).rgb;

                float d = Depth(uv);
                float fade = 1 - smoothstep(_FadeStart, _FadeEnd, d);
                if (fade <= 0) return half4(color, 1);

                //-- same width on any screen size
                float2 px = max(1, _LineWidth * _ScreenSize.y / 1080.0) * _ScreenSize.zw;
                float3 n = SampleSceneNormals(uv);

                //-- silhouette: only drawn on the near side so it stays one pixel thin
                float id = 1.0 / max(d, 0.0001);
                float edgeX = 2 * id - InvDepth(uv - float2(px.x, 0)) - InvDepth(uv + float2(px.x, 0));
                float edgeY = 2 * id - InvDepth(uv - float2(0, px.y)) - InvDepth(uv + float2(0, px.y));
                float silhouette = saturate((max(edgeX, edgeY) / id - 0.04) * 12);

                //-- creases: where the surface turns sharply
                float3 nr = SampleSceneNormals(uv + float2(px.x, 0));
                float3 nu = SampleSceneNormals(uv + float2(0, px.y));
                float3 nl = SampleSceneNormals(uv - float2(px.x, 0));
                float3 nd = SampleSceneNormals(uv - float2(0, px.y));
                float bend = max(1 - dot(n, nr), 1 - dot(n, nu));
                float crease = saturate((bend - _CreaseThreshold) * 3);

                //-- terrain is made of flat triangles but has smooth normals, so its folds show up in
                //-- depth only. needing the normals to change too keeps lines off the sand
                float silBend = max(bend, max(1 - dot(n, nl), 1 - dot(n, nd)));
                silhouette *= smoothstep(0.02, 0.1, silBend);

                //-- dents: this pixel sits deeper than the ring around it (nostrils, cracks, mouth corners)
                float2 r = max(1, _DentRadius * _ScreenSize.y / 1080.0) * _ScreenSize.zw;
                float dent = DentPair(uv, float2(r.x, 0), id) + DentPair(uv, float2(0, r.y), id) + DentPair(uv, r, id) + DentPair(uv, float2(r.x, -r.y), id);
                dent = max(dent * 0.25, 0) / id;
                dent = saturate((dent - _DentThreshold) / _DentThreshold);
                float dentBend = max(max(1 - dot(n, SampleSceneNormals(uv + float2(r.x, 0))), 1 - dot(n, SampleSceneNormals(uv - float2(r.x, 0)))),
                                     max(1 - dot(n, SampleSceneNormals(uv + float2(0, r.y))), 1 - dot(n, SampleSceneNormals(uv - float2(0, r.y)))));
                dent *= smoothstep(0.02, 0.1, dentBend);
                //-- dents are tiny details, only worth drawing up close
                dent *= 1 - smoothstep(_FadeStart * 0.4, _FadeStart * 0.7, d);

                float amount = max(silhouette * _SilhouetteStrength, max(crease * _CreaseStrength, dent * _DentStrength)) * fade;
                //-- darker version of whatever is underneath, softer than black lines
                color = lerp(color, color * _LineColor.rgb, amount);
                return half4(color, 1);
            }
            ENDHLSL
        }
    }
}
