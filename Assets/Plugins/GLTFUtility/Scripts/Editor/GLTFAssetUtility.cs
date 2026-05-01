using System.Collections.Generic;
using System.Linq;
using UnityEditor;
#if !UNITY_2020_2_OR_NEWER
using UnityEditor.Experimental.AssetImporters;
#else
using UnityEditor.AssetImporters;
#endif
using UnityEngine;

namespace Siccity.GLTFUtility {
	/// <summary> Contains methods for saving a gameobject as an asset </summary>
	public static class GLTFAssetUtility {
		public static void SaveToAsset(GameObject root, AnimationClip[] animations, AssetImportContext ctx, ImportSettings settings) {
#if UNITY_2018_2_OR_NEWER
			ctx.AddObjectToAsset("main", root);
			ctx.SetMainObject(root);
#else
			ctx.SetMainAsset("main obj", root);
#endif
			UnwrapParam? unwrapParams = new UnwrapParam()
			{
				angleError = settings.angleError,
				areaError = settings.areaError,
				hardAngle = settings.hardAngle,
				packMargin = settings.packMargin
			};

			MeshRenderer[] renderers = root.GetComponentsInChildren<MeshRenderer>(true);
			SkinnedMeshRenderer[] skinnedRenderers = root.GetComponentsInChildren<SkinnedMeshRenderer>(true);
			MeshFilter[] filters = root.GetComponentsInChildren<MeshFilter>(true);
			AddMeshes(filters, skinnedRenderers, ctx, settings.generateLightmapUVs ? unwrapParams : null);
			AddMaterials(renderers, skinnedRenderers, ctx);
			AddAnimations(animations, ctx, settings.animationSettings);
		}

		public static void AddMeshes(MeshFilter[] filters, SkinnedMeshRenderer[] skinnedRenderers, AssetImportContext ctx, UnwrapParam? lightmapUnwrapInfo) {
			HashSet<Mesh> visitedMeshes = new HashSet<Mesh>();
			for (int i = 0; i < filters.Length; i++) {
				Mesh mesh = filters[i].sharedMesh;
				if (lightmapUnwrapInfo.HasValue) Unwrapping.GenerateSecondaryUVSet(mesh, lightmapUnwrapInfo.Value);
				if (visitedMeshes.Contains(mesh)) continue;
				ctx.AddAsset(mesh.name, mesh);
				visitedMeshes.Add(mesh);
			}
			for (int i = 0; i < skinnedRenderers.Length; i++) {
				Mesh mesh = skinnedRenderers[i].sharedMesh;
				if (visitedMeshes.Contains(mesh)) continue;
				ctx.AddAsset(mesh.name, mesh);
				visitedMeshes.Add(mesh);
			}
		}

		public static void AddAnimations(AnimationClip[] animations, AssetImportContext ctx, AnimationSettings settings) {
			if (animations == null) return;

			// Editor-only animation settings
			foreach (AnimationClip clip in animations) {
				AnimationClipSettings clipSettings = AnimationUtility.GetAnimationClipSettings(clip);
				clipSettings.loopTime = settings.looping;
				AnimationUtility.SetAnimationClipSettings(clip, clipSettings);
			}

			HashSet<AnimationClip> visitedAnimations = new HashSet<AnimationClip>();
			for (int i = 0; i < animations.Length; i++) {
				AnimationClip clip = animations[i];
				if (visitedAnimations.Contains(clip)) continue;
				ctx.AddAsset(clip.name, clip);
				visitedAnimations.Add(clip);
			}
		}

		public static void AddMaterials(MeshRenderer[] renderers, SkinnedMeshRenderer[] skinnedRenderers, AssetImportContext ctx) {
			HashSet<Material> visitedMaterials = new HashSet<Material>();
			HashSet<Texture2D> visitedTextures = new HashSet<Texture2D>();
			for (int i = 0; i < renderers.Length; i++) {
				foreach (Material mat in renderers[i].sharedMaterials) {
					if (mat == GLTFMaterial.defaultMaterial) continue;
					if (visitedMaterials.Contains(mat)) continue;
					if (string.IsNullOrEmpty(mat.name)) mat.name = "material" + visitedMaterials.Count;
					ctx.AddAsset(mat.name, mat);
					visitedMaterials.Add(mat);

					// Add textures
					foreach (Texture2D tex in mat.AllTextures()) {
						// Dont add asset textures
						//if (images[i].isAsset) continue;
						if (visitedTextures.Contains(tex)) continue;
						if (AssetDatabase.Contains(tex)) continue;
						if (string.IsNullOrEmpty(tex.name)) tex.name = "texture" + visitedTextures.Count;
						OptimizeTextureForWebGL(tex);
						ctx.AddAsset(tex.name, tex);
						visitedTextures.Add(tex);
					}
				}
			}
			for (int i = 0; i < skinnedRenderers.Length; i++) {
				foreach (Material mat in skinnedRenderers[i].sharedMaterials) {
					if (visitedMaterials.Contains(mat)) continue;
					if (string.IsNullOrEmpty(mat.name)) mat.name = "material" + visitedMaterials.Count;
					ctx.AddAsset(mat.name, mat);
					visitedMaterials.Add(mat);

					// Add textures
					foreach (Texture2D tex in mat.AllTextures()) {
						// Dont add asset textures
						//if (images[i].isAsset) continue;
						if (visitedTextures.Contains(tex)) continue;
						if (AssetDatabase.Contains(tex)) continue;
						if (string.IsNullOrEmpty(tex.name)) tex.name = "texture" + visitedTextures.Count;
						OptimizeTextureForWebGL(tex);
						ctx.AddAsset(tex.name, tex);
						visitedTextures.Add(tex);
					}
				}
			}
		}

		/// <summary>
		/// 把 GLTFUtility 加载完的 ARGB32+mipmap Texture2D 缩到合理尺寸 + 转 GPU 压缩格式。
		/// 不做这步的话每个化石/矿物 .glb 在 build 里占 192 MB，1.5 GB 包体里 90% 都是它们的纹理。
		/// 改完单个 .glb 从 192 MB → 2-5 MB（45 倍缩减）。
		/// </summary>
		private const int WebGLMaxTextureSize = 1024;
		private static void OptimizeTextureForWebGL(Texture2D tex) {
			if (tex == null) return;
			try {
				// Step 1: Resize if exceeds max
				if (tex.width > WebGLMaxTextureSize || tex.height > WebGLMaxTextureSize) {
					int newW, newH;
					if (tex.width >= tex.height) {
						newW = WebGLMaxTextureSize;
						newH = Mathf.Max(1, tex.height * WebGLMaxTextureSize / tex.width);
					} else {
						newH = WebGLMaxTextureSize;
						newW = Mathf.Max(1, tex.width * WebGLMaxTextureSize / tex.height);
					}

					RenderTexture rt = RenderTexture.GetTemporary(newW, newH, 0, RenderTextureFormat.Default);
					var prev = RenderTexture.active;
					Graphics.Blit(tex, rt);
					RenderTexture.active = rt;

					Texture2D temp = new Texture2D(newW, newH, TextureFormat.RGBA32, false);
					temp.ReadPixels(new Rect(0, 0, newW, newH), 0, 0);
					temp.Apply();
					Color32[] pixels = temp.GetPixels32();
					Object.DestroyImmediate(temp);

					RenderTexture.active = prev;
					RenderTexture.ReleaseTemporary(rt);

					// Resize the original Texture2D in place（保留对它的所有 Material 引用）
					tex.Reinitialize(newW, newH, TextureFormat.RGBA32, false);
					tex.SetPixels32(pixels);
					tex.Apply(false, false);
				}

				// Step 2: Compress to GPU-friendly DXT5Crunched
				// DXT5Crunched 在 WebGL 解码快，体积约 ARGB32 的 1/8
				EditorUtility.CompressTexture(tex, TextureFormat.DXT5Crunched, 50);
			} catch (System.Exception e) {
				Debug.LogWarning($"[GLTFUtility] OptimizeTextureForWebGL 失败 (tex={tex.name}): {e.Message}");
			}
		}

		public static void AddAsset(this AssetImportContext ctx, string identifier, Object obj) {
#if UNITY_2018_2_OR_NEWER
			ctx.AddObjectToAsset(identifier, obj);
#else
			ctx.AddSubAsset(identifier, obj);
#endif
		}

		public static IEnumerable<Texture2D> AllTextures(this Material mat) {
			int[] ids = mat.GetTexturePropertyNameIDs();
			for (int i = 0; i < ids.Length; i++) {
				Texture2D tex = mat.GetTexture(ids[i]) as Texture2D;
				if (tex != null) yield return tex;
			}
		}
	}
}