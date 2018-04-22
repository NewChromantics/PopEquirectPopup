using System.Collections;
using System.Collections.Generic;
using UnityEngine;




[System.Serializable]
public class UnityEvent_Mesh : UnityEngine.Events.UnityEvent<Mesh> { }




public class EquirectMeshPlotter : MonoBehaviour
{
	[Range(0.01f, 0.5f)]
	public float GizmoSize = 0.1f;
	public Color GizmoColour = Color.red;
	public Transform Camera { get { return GameObject.FindObjectOfType<Camera>().transform; } }
	Plane WorldFloorPlane { get { return new Plane(Vector3.up, FloorY); } }
	List<Vector3> ViewDirections;
	public Vector3 RayOrigin { get { return Camera.position; } }
	public UnityEvent_Mesh OnGeneratedMesh;

	float FloorY { get { return 0; } }
	float CameraY { get { return Camera.position.y; } }

	[OnChanged(null,"OnChanged")]
	public bool InvertU = false;
	[Range(-180,180)]
	public float UOffsetDegrees = 0;

	//	if you sample UV's from an equirect directly, they'll bow. Subdivide to get around it (still hacky!)
	[Range(0, 40)]
	public int GeneratedMeshSubDivisions = 0;

	struct PositionAndUv
	{
		public Vector3 Position;
		public Vector2 Uv;
		public Vector3 Direction;

		public PositionAndUv(Vector3 Position, Vector2 Uv,Vector3 Direction)
		{
			this.Position = Position;
			this.Uv = Uv;
			this.Direction = Direction;
		}

		public PositionAndUv LerpTo(PositionAndUv Next,float Time)
		{
			var Prev = this;
			var p = Vector3.Lerp(Prev.Position, Next.Position, Time);
			var u = Vector2.Lerp(Prev.Uv, Next.Uv, Time);
			//	bad vector lerp. slerp this if it's ever used!
			var d = Vector3.Lerp(Prev.Direction, Next.Direction, Time);
			var New = new PositionAndUv(p, u, d);
			return New;
		}
	};

	public void AddViewDirectionPoint(Vector3 ViewDirection)
	{
		//	transform direction by camera rotation
		Pop.AllocIfNull(ref ViewDirections);
		ViewDirections.Add(ViewDirection);
		OnChanged();
		Debug.Log("Added view dir");
	}

	int? FindClosestViewDirectionPoint(Vector3 MatchViewDirection,float MinScore)
	{
		Pop.AllocIfNull(ref ViewDirections);

		//	closest to 1 is closest to vector
		int? ClosestDotIndex = null;
		float ClosestDot = 0;
		float MinDot = 1 - MinScore;
		for (int i = 0; i < ViewDirections.Count;	i++ )
		{
			var ViewDir = ViewDirections[i];
			var Dot = Vector3.Dot(ViewDir, MatchViewDirection);

			if (Dot < MinDot)
				continue;

			if ( ClosestDotIndex.HasValue )
			{
				if (ClosestDot > Dot)
					continue;
			}

			ClosestDotIndex = i;
			ClosestDot = Dot;
		}
		return ClosestDotIndex;
	}


	public bool RemoveViewDirectionPoint(Vector3 ViewDirection,float MinScore)
	{
		//	transform direction by camera rotation
		var NearIndex = FindClosestViewDirectionPoint(ViewDirection, MinScore);
		if (!NearIndex.HasValue)
			return false;

		ViewDirections.RemoveAt(NearIndex.Value);
		OnChanged();
		return true;
	}

	List<Ray> GetWorldRays()
	{
		Pop.AllocIfNull(ref ViewDirections);
		var WorldRays = new List<Ray>();
		var RayOrigin = this.RayOrigin;
		foreach (var ViewDirection in ViewDirections)
		{
			var Ray = new Ray(RayOrigin, ViewDirection);
			WorldRays.Add(Ray);
		}
		return WorldRays;
	}

	Vector2 GetEquirectUvFromView(Vector3 View3)
	{
		View3 = View3.normalized;
		const float UNITY_PI = Mathf.PI;
		var longlat = new Vector2(Mathf.Atan2(View3.x, View3.z) + UNITY_PI, Mathf.Acos(-View3.y));

		var u = longlat.x / (2.0f * UNITY_PI);
		var v = longlat.y / (UNITY_PI);

		if (InvertU)
			u = 1 - u;
		u += UOffsetDegrees / 260.0f;

		return new Vector2(u,v);
	}

	List<PositionAndUv?> GetWorldPoints()
	{
		var WorldRays = GetWorldRays();
		var FloorPlane = this.WorldFloorPlane;
		var FloorPositionAndUvs = new List<PositionAndUv?>();

		foreach (var WorldRay in WorldRays)
		{
			var Uv = GetEquirectUvFromView(WorldRay.direction);
			
			//	find floor hit
			float HitTime;
			if (!FloorPlane.Raycast(WorldRay, out HitTime))
			{
				Debug.LogWarning("View direction " + WorldRay.direction + " doesnt hit floor");
				FloorPositionAndUvs.Add( null );
			}
			else 
			{
				var FloorPos = WorldRay.GetPoint(HitTime);
				FloorPositionAndUvs.Add( new PositionAndUv(FloorPos,Uv,WorldRay.direction));
			}
		}

		return FloorPositionAndUvs;
	}


	void OnDrawGizmos()
	{
		Gizmos.matrix = Matrix4x4.identity;
		Gizmos.color = GizmoColour;

		var FloorPositions = GetWorldPoints();
		foreach ( var FloorPos in FloorPositions )
		{
			if (!FloorPos.HasValue)
				continue;
			var Pos = FloorPos.Value.Position;
			Gizmos.DrawSphere( Pos, GizmoSize );
		}
	}

	public Mesh GenerateMesh()
	{
		var WorldPoints = GetWorldPoints();
		if (WorldPoints.Count < 2)
			throw new System.Exception("Not enough points to generate mesh");

		//	generate a triangle fan
		var Positions = new List<Vector3>();
		var Uv0s = new List<Vector2>();
		var Directions = new List<Vector3>();
		var TriangleIndexes = new List<int>();

		System.Func<PositionAndUv,int> AddVertex = (Vert) =>
		{
			var Index = Positions.Count;
			Positions.Add(Vert.Position);
			Uv0s.Add(Vert.Uv);
			Directions.Add(Vert.Direction);
			return Index;
		};
		System.Action<PositionAndUv,PositionAndUv,PositionAndUv> AddTriangle = (a,b,c) =>
		{
			var v0 = AddVertex(a);
			var v1 = AddVertex(b);
			var v2 = AddVertex(c);
			TriangleIndexes.Add(v0);
			TriangleIndexes.Add(v1);
			TriangleIndexes.Add(v2);
		};

		//	gr: should this be floorY?
		var CenterVertex = new PositionAndUv(Vector3.zero, GetEquirectUvFromView(Vector3.down), Vector3.down );
		for (int i = 0; i < WorldPoints.Count;	i++ )
		{
			try
			{
				var Prev = WorldPoints[i].Value;
				var Next = WorldPoints[(i + 1) % WorldPoints.Count].Value;

				float Step = 1.0f / (GeneratedMeshSubDivisions + 1);
				for (int subdiv = 0; subdiv <=GeneratedMeshSubDivisions;subdiv++ )
				{
					float ta = (subdiv + 0) * Step;
					float tb = (subdiv + 1) * Step;
					var a = Prev.LerpTo(Next, ta);
					var b = Prev.LerpTo(Next, tb);
					AddTriangle(CenterVertex, a, b);
				}
			}
			catch
			{}
		}

		var Mesh = new Mesh();
		Mesh.name = this.name;
		Mesh.SetVertices(Positions);
		Mesh.SetUVs(0, Uv0s);
		Mesh.SetUVs(1, Directions);
		Mesh.SetTriangles(TriangleIndexes.ToArray(),0);
		return Mesh;
	}

	void OnChanged()
	{
		try
		{
			var Mesh = GenerateMesh();
			OnGeneratedMesh.Invoke(Mesh);
		}
		catch(System.Exception e)
		{
			Debug.LogException(e);
		}
	}
}

