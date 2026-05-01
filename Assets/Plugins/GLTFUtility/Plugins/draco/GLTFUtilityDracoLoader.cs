// Stub 替代品：原版用 [DllImport("__Internal")] 引用 dracodec_unity native 库，
// 但本项目根本没装这个 native lib，且所有 .glb 资源都不带 KHR_draco_mesh_compression
// extension，运行时永远不会调用 LoadMesh()。保留类型定义只为让 GLTFMesh.cs
// 引用 DracoMeshCompression 的代码能编译过；任何实际调用会抛异常提示。
//
// 如以后真要支持 Draco 压缩 .glb，需要：
//   1. 装 dracodec_unity native lib（.bc/.so/wasm 等多平台二进制）
//   2. 把这个文件还原为 PackageCache 原版

using UnityEngine;

namespace Siccity.GLTFUtility
{
    public class GLTFUtilityDracoLoader
    {
        public struct MeshAttributes
        {
            public int POSITION, NORMAL, TEXCOORD, JOINTS_0, WEIGHTS_0, COLOR;
            public MeshAttributes(int p, int n, int t, int j, int w, int c)
            {
                POSITION = p; NORMAL = n; TEXCOORD = t;
                JOINTS_0 = j; WEIGHTS_0 = w; COLOR = c;
            }
        }

        public class AsyncMesh
        {
            public int[] tris;
            public Vector3[] verts;
            public Vector2[] uv;
            public Vector3[] norms;
            public BoneWeight[] boneWeights;
            public Color[] colors;
        }

        public AsyncMesh LoadMesh(byte[] buffer, MeshAttributes attribs)
        {
            Debug.LogWarning("[GLTFUtilityDracoLoader] Draco 解压未启用——本项目 stub 版只保留类型，不解码。" +
                             "如果 .glb 用了 KHR_draco_mesh_compression extension，需要装 dracodec_unity native lib。");
            return null;
        }
    }
}
