using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[System.Serializable]
public class DrawTriangleShader
{
	//	unity caches sizes of array uniforms, so we pre-make certain sizes
	const int MaxTriangleCount = 10;

	public Shader BlitShader;
	public string TriangleUvs_Uniform = "TriangleUvs";
	public string TriangleColours_Uniform = "TriangleColours";


	public void SetUniforms(Material DrawTriangleMaterial,Triangle2 TriangleUvs,Triangle3 TriangleColours)
	{
		var Uvs = new List<Vector2>();
		Uvs.Add(TriangleUvs.a);
		Uvs.Add(TriangleUvs.b);
		Uvs.Add(TriangleUvs.c);

		var Colours = new List<Color>();
		Colours.Add(new Color(TriangleColours.a.x, TriangleColours.a.y, TriangleColours.a.z, 1));
		Colours.Add(new Color(TriangleColours.b.x, TriangleColours.b.y, TriangleColours.b.z, 1));
		Colours.Add(new Color(TriangleColours.c.x, TriangleColours.c.y, TriangleColours.c.z, 1));

		SetUniforms(DrawTriangleMaterial, Uvs, Colours);
	}

	public void SetUniforms(Material DrawTriangleMaterial,List<Vector2> TriangleUvs, List<Color> TriangleColours)
	{
		var TriangleUvs4s = new List<Vector4>();
		foreach (var Uv2 in TriangleUvs)
			TriangleUvs4s.Add(Uv2.xy00());

		var TriangleColour4s = new List<Vector4>();
		foreach (var Colour in TriangleColours)
			TriangleColour4s.Add(Colour.GetVector4());

		DrawTriangleMaterial.SetVectorArray(TriangleUvs_Uniform, TriangleUvs4s);
		DrawTriangleMaterial.SetVectorArray(TriangleColours_Uniform, TriangleColour4s);
	}
}



[RequireComponent(typeof(EquirectMeshPlotter))]
public class EquirectMeshEditor : MonoBehaviour
{
	[Range(0, 1)]
	public float RemoveMaxDistance = 0.1f;

	EquirectMeshPlotter	Plotter	{ get { return GetComponent<EquirectMeshPlotter>(); }}
	public PopX.IO.ImageFileType ExportTextureMapFormat = PopX.IO.ImageFileType.jpg;
	public int ExportTextureMapSize = 1024;
	public DrawTriangleShader ExportTextureMapShader = new DrawTriangleShader();

	public void AddScreenPoint(Vector2 ScreenPos)
	{
		var Ray = Plotter.Camera.GetComponent<Camera>().ScreenPointToRay(ScreenPos.xy0());
		Plotter.AddViewDirectionPoint(Ray.direction);
	}

	public void RemoveScreenPoint(Vector2 ScreenPos)
	{
		var Ray = Plotter.Camera.GetComponent<Camera>().ScreenPointToRay(ScreenPos.xy0());
		if (!Plotter.RemoveViewDirectionPoint(Ray.direction, RemoveMaxDistance))
			OnDidntRemove();
	}

	void OnDidntRemove()
	{
		UnityEditor.EditorApplication.Beep();
	}

	public void ExportMesh()
	{
		try
		{
			//	make a mesh
			var mesh = Plotter.GenerateMesh();

			//	make a texture map and get it's new uvs
			Vector2[] AtlasUvs;
			var PositionAtlas = GeneratePositionTextureMap(mesh, out AtlasUvs, ExportTextureMapSize);
			mesh.uv = AtlasUvs;
			//var ProjectedTexture = GenerateProjectedTextureMap(PositionAtlas);
			var ProjectedTexture = PositionAtlas;
			var ProjectedTexture2D = PopX.Textures.GetTexture2D(ProjectedTexture,true);

			string Filename;
			var WriteMeshLine = PopX.IO.GetFileWriteLineFunction(out Filename, "Exported Obj", "Mesh", PopX.WavefrontObj.MeshFileExtension);

			var TextureFilename = System.IO.Path.ChangeExtension(Filename, PopX.IO.GetImageFormatExtension(ExportTextureMapFormat));
			var MaterialFilename = System.IO.Path.ChangeExtension(Filename, PopX.WavefrontObj.MaterialFileExtension);
			var WriteMaterialLine = PopX.IO.GetFileWriteLineFunction(MaterialFilename);

			var Material = new PopX.WavefrontObj.ObjMaterial("ProjectionMap",TextureFilename);

			PopX.WavefrontObj.Export(WriteMeshLine, mesh, Matrix4x4.identity, Material, MaterialFilename );
			PopX.WavefrontObj.Export(WriteMaterialLine, Material);
			var WriteImage = PopX.IO.GetFileWriteImageFunction(ExportTextureMapFormat);
			WriteImage(TextureFilename,ProjectedTexture2D);

			UnityEditor.EditorUtility.RevealInFinder(Filename);
		}
		catch(System.Exception e)
		{
			ShowUiError.ShowError(e.Message);
		}
	}


	void RenderTriangleAtlas(ref RenderTexture AtlasTexture, List<Triangle2> AtlasTriangles, List<Triangle3> MeshTriangles)
	{
		//	render each triangle to atlas (can probably do all of these at once with an ortho camera)

		//	make a temp copy
		var TempTexture = new RenderTexture(AtlasTexture);
		PopX.Textures.ClearTexture(AtlasTexture, Color.black);
		PopX.Textures.ClearTexture(TempTexture, Color.black);

		Pop.AllocIfNull(ref ExportTextureMapShader);
		var Material = new Material(ExportTextureMapShader.BlitShader);

		for (int t = 0; t < AtlasTriangles.Count; t++)
		{
			ExportTextureMapShader.SetUniforms(Material, AtlasTriangles[t], MeshTriangles[t]);
			Graphics.Blit(TempTexture, AtlasTexture, Material);
			Graphics.Blit(AtlasTexture, TempTexture);
		}

	}

	//	generate a loat texture map which contains the XYZ(Valid) position of every triangle
	Texture GeneratePositionTextureMap(Mesh mesh, out Vector2[] VertexAtlasUvs,int TextureSize)
	{
		//	generate atlas
		var MeshTriangles = new List<Triangle3>();
		var Positions = mesh.vertices;
		var TriangleIndexes = mesh.triangles;

		if ( Positions.Length != TriangleIndexes.Length )
			throw new System.Exception("Mesh vertexes need to be unshared. To use shared vertexes we may need to split individual triangles depending on atlasing");

		System.Action<int, int, int> AddTriangleByVertexIndexes = (a, b, c) =>
		{
			var Triangle = new Triangle3();
			Triangle.a = Positions[a];
			Triangle.b = Positions[b];
			Triangle.c = Positions[c];
			MeshTriangles.Add(Triangle);
		};
		for (int t = 0; t < TriangleIndexes.Length; t += 3)
		{
			var ia = TriangleIndexes[t + 0];
			var ib = TriangleIndexes[t + 1];
			var ic = TriangleIndexes[t + 2];
			AddTriangleByVertexIndexes(ia, ib, ic);
		}

		var AtlasTriangles = PopTrianglePacker.AllocateTriangleAtlases(MeshTriangles);

		var AtlasTexture = new RenderTexture(TextureSize, TextureSize, 0, RenderTextureFormat.ARGBFloat);
		
		RenderTriangleAtlas(ref AtlasTexture, AtlasTriangles, MeshTriangles);

		//	need to give vertexes new uvs! one per vertex
		//	one 
		VertexAtlasUvs = new Vector2[Positions.Length];
		for (int t = 0; t < TriangleIndexes.Length/3; t++)
		{
			var TriangleIndex = t * 3;
			var VertexIndexa = TriangleIndexes[TriangleIndex + 0];
			var VertexIndexb = TriangleIndexes[TriangleIndex + 1];
			var VertexIndexc = TriangleIndexes[TriangleIndex + 2];
			//	get uvs of triangle
			var uva = AtlasTriangles[t].a;
			var uvb = AtlasTriangles[t].b;
			var uvc = AtlasTriangles[t].c;

			VertexAtlasUvs[VertexIndexa] = uva;
			VertexAtlasUvs[VertexIndexb] = uvb;
			VertexAtlasUvs[VertexIndexc] = uvc;
		}

		return AtlasTexture;
	}


	Texture GenerateProjectedTextureMap(Texture PositionTextureMap)
	{
		throw new System.Exception("todo");
	}
}

