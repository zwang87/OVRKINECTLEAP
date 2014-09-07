using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Kinect = Windows.Kinect;

public class BodySourceView : MonoBehaviour 
{
	public Material BoneMaterial;
	public GameObject BodySourceManager;

	public Vector3 handRightPos;// = new Vector3 (0, 0, 0);
	public Vector3 handLeftPos;// = new Vector3 (0, 0, 0);
	
	private Dictionary<ulong, GameObject> _Bodies = new Dictionary<ulong, GameObject>();
	private BodySourceManager _BodyManager;
	
	private Dictionary<Kinect.JointType, Kinect.JointType> _BoneMap = new Dictionary<Kinect.JointType, Kinect.JointType>()
	{
		{ Kinect.JointType.FootLeft, Kinect.JointType.AnkleLeft },
		{ Kinect.JointType.AnkleLeft, Kinect.JointType.KneeLeft },
		{ Kinect.JointType.KneeLeft, Kinect.JointType.HipLeft },
		{ Kinect.JointType.HipLeft, Kinect.JointType.SpineBase },
		
		{ Kinect.JointType.FootRight, Kinect.JointType.AnkleRight },
		{ Kinect.JointType.AnkleRight, Kinect.JointType.KneeRight },
		{ Kinect.JointType.KneeRight, Kinect.JointType.HipRight },
		{ Kinect.JointType.HipRight, Kinect.JointType.SpineBase },
		
		{ Kinect.JointType.HandTipLeft, Kinect.JointType.HandLeft },
		{ Kinect.JointType.ThumbLeft, Kinect.JointType.HandLeft },
		{ Kinect.JointType.HandLeft, Kinect.JointType.WristLeft },
		{ Kinect.JointType.WristLeft, Kinect.JointType.ElbowLeft },
		{ Kinect.JointType.ElbowLeft, Kinect.JointType.ShoulderLeft },
		{ Kinect.JointType.ShoulderLeft, Kinect.JointType.SpineShoulder },
		
		{ Kinect.JointType.HandTipRight, Kinect.JointType.HandRight },
		{ Kinect.JointType.ThumbRight, Kinect.JointType.HandRight },
		{ Kinect.JointType.HandRight, Kinect.JointType.WristRight },
		{ Kinect.JointType.WristRight, Kinect.JointType.ElbowRight },
		{ Kinect.JointType.ElbowRight, Kinect.JointType.ShoulderRight },
		{ Kinect.JointType.ShoulderRight, Kinect.JointType.SpineShoulder },
		
		{ Kinect.JointType.SpineBase, Kinect.JointType.SpineMid },
		{ Kinect.JointType.SpineMid, Kinect.JointType.SpineShoulder },
		{ Kinect.JointType.SpineShoulder, Kinect.JointType.Neck },
		{ Kinect.JointType.Neck, Kinect.JointType.Head },
	};
	
	void Update () 
	{
		if (BodySourceManager == null)
		{
			return;
		}
		
		_BodyManager = BodySourceManager.GetComponent<BodySourceManager>();
		if (_BodyManager == null)
		{
			return;
		}
		
		Kinect.Body[] data = _BodyManager.GetData();
		if (data == null)
		{
			return;
		}
		
		List<ulong> trackedIds = new List<ulong>();
		foreach(var body in data)
		{
			if (body == null)
			{
				continue;
			}
			
			if(body.IsTracked)
			{
				trackedIds.Add (body.TrackingId);
			}
		}
		
		List<ulong> knownIds = new List<ulong>(_Bodies.Keys);
		
		// First delete untracked bodies
		foreach(ulong trackingId in knownIds)
		{
			if(!trackedIds.Contains(trackingId))
			{
				Destroy(_Bodies[trackingId]);
				_Bodies.Remove(trackingId);
			}
		}
		
		foreach(var body in data)
		{
			if (body == null)
			{
				continue;
			}
			
			if(body.IsTracked)
			{
				if(!_Bodies.ContainsKey(body.TrackingId))
				{
					_Bodies[body.TrackingId] = CreateBodyObject(body.TrackingId);
				}
				
				RefreshBodyObject(body, _Bodies[body.TrackingId]);
			}
		}
	}
	
	private GameObject CreateBodyObject(ulong id)
	{
		GameObject body = new GameObject("Body:" + id);
		
		for (Kinect.JointType jt = Kinect.JointType.SpineBase; jt <= Kinect.JointType.ThumbRight; jt++)
		{
			GameObject jointObj = new GameObject();// = GameObject.CreatePrimitive(PrimitiveType.Cube);

			//jointObj.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
			
			LineRenderer lr = jointObj.AddComponent<LineRenderer>();
			lr.SetVertexCount(2);
			lr.material = BoneMaterial;
			lr.SetWidth(0.05f, 0.05f);
			
			jointObj.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);
			jointObj.name = jt.ToString();
			jointObj.transform.parent = body.transform;
		}
		
		return body;
	}


	
	private void RefreshBodyObject(Kinect.Body body, GameObject bodyObject)
	{
		Vector3 offset = new Vector3 (0, 0, 0);

		Kinect.Joint head = body.Joints [Kinect.JointType.Head];



		offset = GameObject.Find ("CameraLeft").transform.position - GetVector3FromJoint (head);

		for (Kinect.JointType jt = Kinect.JointType.SpineBase; jt <= Kinect.JointType.ThumbRight; jt++)
		{
			Vector3 spineBase = GetVector3FromJoint(body.Joints[Kinect.JointType.SpineBase]);

			if(jt < Kinect.JointType.HipLeft || jt > Kinect.JointType.FootRight)
			{
				Kinect.Joint sourceJoint = body.Joints[jt];
				Kinect.Joint? targetJoint = null;
				
				if(_BoneMap.ContainsKey(jt))
				{
					targetJoint = body.Joints[_BoneMap[jt]];
				}

				Transform jointObj = bodyObject.transform.FindChild(jt.ToString());
				Vector3 origin = GetVector3FromJoint(sourceJoint);

				if(jt == Kinect.JointType.HandRight)
				{
					handRightPos = origin;
				}
				else if(jt == Kinect.JointType.HandLeft)
				{
					handLeftPos = origin;
				}

				float tempY = 2 * spineBase.x - origin.x;

				jointObj.localPosition = new Vector3(tempY, origin.y, origin.z);

				LineRenderer lr = jointObj.GetComponent<LineRenderer>();
				if(targetJoint.HasValue)
				{
					Vector3 originTarget =  GetVector3FromJoint(targetJoint.Value);
					float tempTargetX = 2 * spineBase.x - originTarget.x;
					Vector3 targetJointPos = new Vector3(tempTargetX, originTarget.y, originTarget.z);
					lr.SetPosition(0, jointObj.localPosition+offset);
					lr.SetPosition(1, targetJointPos+offset);
					lr.SetColors(GetColorForState (sourceJoint.TrackingState), GetColorForState(targetJoint.Value.TrackingState));
				}
				else
				{
					lr.enabled = false;
				}

			}


		}
		/*
		if (hasValue) 
		{
			Transform jointObj = bodyObject.transform;
			LineRenderer lr = jointObj.GetComponent<LineRenderer>();
			for(int i = 0; i < numJoints-1; i++)
			{
				lr.SetPosition(0, srcV[i]);
				lr.SetPosition(1, srcV[i+1]);
			}

		}
		*/
	}
	
	private static Color GetColorForState(Kinect.TrackingState state)
	{
		switch (state)
		{
		case Kinect.TrackingState.Tracked:
			return Color.green;
			
		case Kinect.TrackingState.Inferred:
			return Color.red;
			
		default:
			return Color.black;
		}
	}
	
	private static Vector3 GetVector3FromJoint(Kinect.Joint joint)
	{
		return new Vector3(joint.Position.X * 2 , joint.Position.Y * 2, joint.Position.Z* 2);
	}
}
