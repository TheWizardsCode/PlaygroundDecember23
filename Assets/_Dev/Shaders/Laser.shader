// Made with Amplify Shader Editor v1.9.2.2
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "WizardsCode/Laser"
{
	Properties
	{
		[HDR]_Colour("Colour", Color) = (1,0,0,0)
		_AnimationSpeed("Animation Speed", Vector) = (-0.5,0,0,0)
		[HDR]_TextureSample0("Texture Sample 0", 2D) = "white" {}
		_EmissionStrength("Emission Strength", Range( 0 , 1)) = 0.5
		_NoiseScale("Noise Scale", Float) = 3
		_NoiseSpeed("Noise Speed", Vector) = (-0.5,0,0,0)
		_NoisePower("Noise Power", Float) = 2
		_NoiseAmount("Noise Amount", Range( 0 , 1)) = 0.5
		_DissolveAmount("Dissolve Amount", Range( 0 , 1)) = 0.5
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "Transparent"  "Queue" = "Transparent+0" "IgnoreProjector" = "True" "IsEmissive" = "true"  }
		Cull Back
		CGINCLUDE
		#include "UnityShaderVariables.cginc"
		#include "UnityPBSLighting.cginc"
		#include "Lighting.cginc"
		#pragma target 3.0
		struct Input
		{
			float2 uv_texcoord;
		};

		uniform float4 _Colour;
		uniform sampler2D _TextureSample0;
		uniform float2 _AnimationSpeed;
		uniform float2 _NoiseSpeed;
		uniform float _NoiseScale;
		uniform float _NoisePower;
		uniform float _NoiseAmount;
		uniform float _DissolveAmount;
		uniform float _EmissionStrength;


		float3 mod2D289( float3 x ) { return x - floor( x * ( 1.0 / 289.0 ) ) * 289.0; }

		float2 mod2D289( float2 x ) { return x - floor( x * ( 1.0 / 289.0 ) ) * 289.0; }

		float3 permute( float3 x ) { return mod2D289( ( ( x * 34.0 ) + 1.0 ) * x ); }

		float snoise( float2 v )
		{
			const float4 C = float4( 0.211324865405187, 0.366025403784439, -0.577350269189626, 0.024390243902439 );
			float2 i = floor( v + dot( v, C.yy ) );
			float2 x0 = v - i + dot( i, C.xx );
			float2 i1;
			i1 = ( x0.x > x0.y ) ? float2( 1.0, 0.0 ) : float2( 0.0, 1.0 );
			float4 x12 = x0.xyxy + C.xxzz;
			x12.xy -= i1;
			i = mod2D289( i );
			float3 p = permute( permute( i.y + float3( 0.0, i1.y, 1.0 ) ) + i.x + float3( 0.0, i1.x, 1.0 ) );
			float3 m = max( 0.5 - float3( dot( x0, x0 ), dot( x12.xy, x12.xy ), dot( x12.zw, x12.zw ) ), 0.0 );
			m = m * m;
			m = m * m;
			float3 x = 2.0 * frac( p * C.www ) - 1.0;
			float3 h = abs( x ) - 0.5;
			float3 ox = floor( x + 0.5 );
			float3 a0 = x - ox;
			m *= 1.79284291400159 - 0.85373472095314 * ( a0 * a0 + h * h );
			float3 g;
			g.x = a0.x * x0.x + h.x * x0.y;
			g.yz = a0.yz * x12.xz + h.yz * x12.yw;
			return 130.0 * dot( m, g );
		}


		void surf( Input i , inout SurfaceOutputStandard o )
		{
			float simplePerlin2D82 = snoise( ( i.uv_texcoord + ( _Time.y * _NoiseSpeed ) )*_NoiseScale );
			simplePerlin2D82 = simplePerlin2D82*0.5 + 0.5;
			float temp_output_87_0 = pow( simplePerlin2D82 , _NoisePower );
			float2 temp_cast_0 = (temp_output_87_0).xx;
			float2 lerpResult89 = lerp( i.uv_texcoord , temp_cast_0 , ( _NoiseAmount * 0.1 ));
			float2 panner53 = ( _Time.y * _AnimationSpeed + lerpResult89);
			float4 tex2DNode64 = tex2D( _TextureSample0, panner53 );
			float4 lerpResult94 = lerp( tex2DNode64 , ( tex2DNode64 * temp_output_87_0 ) , _DissolveAmount);
			o.Albedo = ( _Colour * lerpResult94 ).rgb;
			o.Emission = ( ( 1.0 - tex2DNode64 ) * ( _EmissionStrength * 0.5 ) * _Colour ).rgb;
			o.Alpha = lerpResult94.r;
		}

		ENDCG
		CGPROGRAM
		#pragma surface surf Standard alpha:fade keepalpha fullforwardshadows exclude_path:deferred 

		ENDCG
		Pass
		{
			Name "ShadowCaster"
			Tags{ "LightMode" = "ShadowCaster" }
			ZWrite On
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 3.0
			#pragma multi_compile_shadowcaster
			#pragma multi_compile UNITY_PASS_SHADOWCASTER
			#pragma skip_variants FOG_LINEAR FOG_EXP FOG_EXP2
			#include "HLSLSupport.cginc"
			#if ( SHADER_API_D3D11 || SHADER_API_GLCORE || SHADER_API_GLES || SHADER_API_GLES3 || SHADER_API_METAL || SHADER_API_VULKAN )
				#define CAN_SKIP_VPOS
			#endif
			#include "UnityCG.cginc"
			#include "Lighting.cginc"
			#include "UnityPBSLighting.cginc"
			sampler3D _DitherMaskLOD;
			struct v2f
			{
				V2F_SHADOW_CASTER;
				float2 customPack1 : TEXCOORD1;
				float3 worldPos : TEXCOORD2;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};
			v2f vert( appdata_full v )
			{
				v2f o;
				UNITY_SETUP_INSTANCE_ID( v );
				UNITY_INITIALIZE_OUTPUT( v2f, o );
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO( o );
				UNITY_TRANSFER_INSTANCE_ID( v, o );
				Input customInputData;
				float3 worldPos = mul( unity_ObjectToWorld, v.vertex ).xyz;
				half3 worldNormal = UnityObjectToWorldNormal( v.normal );
				o.customPack1.xy = customInputData.uv_texcoord;
				o.customPack1.xy = v.texcoord;
				o.worldPos = worldPos;
				TRANSFER_SHADOW_CASTER_NORMALOFFSET( o )
				return o;
			}
			half4 frag( v2f IN
			#if !defined( CAN_SKIP_VPOS )
			, UNITY_VPOS_TYPE vpos : VPOS
			#endif
			) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID( IN );
				Input surfIN;
				UNITY_INITIALIZE_OUTPUT( Input, surfIN );
				surfIN.uv_texcoord = IN.customPack1.xy;
				float3 worldPos = IN.worldPos;
				half3 worldViewDir = normalize( UnityWorldSpaceViewDir( worldPos ) );
				SurfaceOutputStandard o;
				UNITY_INITIALIZE_OUTPUT( SurfaceOutputStandard, o )
				surf( surfIN, o );
				#if defined( CAN_SKIP_VPOS )
				float2 vpos = IN.pos;
				#endif
				half alphaRef = tex3D( _DitherMaskLOD, float3( vpos.xy * 0.25, o.Alpha * 0.9375 ) ).a;
				clip( alphaRef - 0.01 );
				SHADOW_CASTER_FRAGMENT( IN )
			}
			ENDCG
		}
	}
	Fallback "Diffuse"
	CustomEditor "ASEMaterialInspector"
}
/*ASEBEGIN
Version=19202
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;85;-1509.526,241.999;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleAddOpNode;86;-1322.526,227.999;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleTimeNode;37;-1722.38,68.3822;Inherit;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;40;-1754.38,-226.6178;Inherit;True;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.Vector2Node;54;-918.38,-171.6178;Inherit;False;Property;_AnimationSpeed;Animation Speed;1;0;Create;True;0;0;0;False;0;False;-0.5,0;-0.3,0;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.RangedFloatNode;83;-1330.526,372.999;Inherit;False;Property;_NoiseScale;Noise Scale;4;0;Create;True;0;0;0;False;0;False;3;3.7;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.Vector2Node;84;-1783.526,389.999;Inherit;False;Property;_NoiseSpeed;Noise Speed;5;0;Create;True;0;0;0;False;0;False;-0.5,0;-0.3,0;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.NoiseGeneratorNode;82;-1096.526,324.999;Inherit;True;Simplex2D;True;False;2;0;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;88;-1065.526,568.999;Inherit;False;Property;_NoisePower;Noise Power;6;0;Create;True;0;0;0;False;0;False;2;1.59;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;45;738.62,-256.6178;Inherit;True;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;93;82.47363,378.999;Inherit;True;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.LerpOp;89;-588.5264,79.99902;Inherit;True;3;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;91;-811.5264,150.999;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0.1;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;90;-1173.526,159.999;Inherit;False;Property;_NoiseAmount;Noise Amount;7;0;Create;True;0;0;0;False;0;False;0.5;0.146;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.PowerNode;87;-845.5264,335.999;Inherit;True;False;2;0;FLOAT;0;False;1;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.PannerNode;53;-392.38,-214.6178;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SamplerNode;64;-217.4836,-236.3824;Inherit;True;Property;_TextureSample0;Texture Sample 0;2;1;[HDR];Create;True;0;0;0;False;0;False;-1;None;bb9e18de7dac0d64eb057f2f74706cf0;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.LerpOp;94;396.4736,418.999;Inherit;True;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;76;674.4735,195.999;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0.5;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;77;342.4735,234.999;Inherit;False;Property;_EmissionStrength;Emission Strength;3;0;Create;True;0;0;0;False;0;False;0.5;0.085;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;73;379.4735,31.99902;Inherit;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;95;114.4736,619.999;Inherit;False;Property;_DissolveAmount;Dissolve Amount;8;0;Create;True;0;0;0;False;0;False;0.5;0.669;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;0;1126,-150;Float;False;True;-1;2;ASEMaterialInspector;0;0;Standard;WizardsCode/Laser;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;False;False;False;False;False;False;Back;0;False;;0;False;;False;0;False;;0;False;;False;0;Transparent;0.5;True;True;0;False;Transparent;;Transparent;ForwardOnly;12;all;True;True;True;True;0;False;;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;2;15;10;25;False;0.5;True;2;5;False;;10;False;;0;0;False;;0;False;;0;False;;0;False;;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;True;Relative;0;;-1;-1;-1;-1;0;False;0;0;False;;-1;0;False;;0;0;0;False;0.1;False;;0;False;;False;17;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;16;FLOAT4;0,0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
Node;AmplifyShaderEditor.ColorNode;44;360.62,-246.6178;Inherit;False;Property;_Colour;Colour;0;1;[HDR];Create;True;0;0;0;False;0;False;1,0,0,0;16,4.432203,0,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;74;861.4735,29.99902;Inherit;False;3;3;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;2;COLOR;0,0,0,0;False;1;COLOR;0
WireConnection;85;0;37;0
WireConnection;85;1;84;0
WireConnection;86;0;40;0
WireConnection;86;1;85;0
WireConnection;82;0;86;0
WireConnection;82;1;83;0
WireConnection;45;0;44;0
WireConnection;45;1;94;0
WireConnection;93;0;64;0
WireConnection;93;1;87;0
WireConnection;89;0;40;0
WireConnection;89;1;87;0
WireConnection;89;2;91;0
WireConnection;91;0;90;0
WireConnection;87;0;82;0
WireConnection;87;1;88;0
WireConnection;53;0;89;0
WireConnection;53;2;54;0
WireConnection;53;1;37;0
WireConnection;64;1;53;0
WireConnection;94;0;64;0
WireConnection;94;1;93;0
WireConnection;94;2;95;0
WireConnection;76;0;77;0
WireConnection;73;0;64;0
WireConnection;0;0;45;0
WireConnection;0;2;74;0
WireConnection;0;9;94;0
WireConnection;74;0;73;0
WireConnection;74;1;76;0
WireConnection;74;2;44;0
ASEEND*/
//CHKSM=5DBFA65D179E4511ACD249E0CBBCA98290343704