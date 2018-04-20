using System.Collections;
using System.Collections.Generic;
using UnityEngine;




[RequireComponent(typeof(EquirectMeshPlotter))]
public class EquirectMeshEditor : MonoBehaviour
{
	[Range(0, 1)]
	public float RemoveMaxDistance = 0.1f;

	EquirectMeshPlotter	Plotter	{ get { return GetComponent<EquirectMeshPlotter>(); }}


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
			var Mesh = Plotter.GenerateMesh();
			string Filename;
			var WriteLine = PopX.IO.GetFileWriteLineFunction(out Filename,"Exported Obj", "Mesh", PopX.WavefrontObj.FileExtension);
			PopX.WavefrontObj.Export(WriteLine, Mesh, Matrix4x4.identity);
			UnityEditor.EditorUtility.RevealInFinder(Filename);
		}
		catch(System.Exception e)
		{
			ShowUiError.ShowError(e.Message);
		}
	}
}

