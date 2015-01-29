using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System;

public class WebApiConsulter : MonoBehaviour
{
		private Quaternion tposeSpineMid;
		private Quaternion tposeNeck;
		private Quaternion tposeHead;
		private Quaternion tposeLeftShoulder;
		private Quaternion tposeLeftElbow;
		private Quaternion tposeLeftWrist;
		private Quaternion tposeRightShoulder;
		private Quaternion tposeRightElbow;
		private Quaternion tposeRightWrist;
		private NetworkStream serverStream;

		void Start ()
		{
				System.Net.Sockets.TcpClient clientSocket = new System.Net.Sockets.TcpClient ();
				clientSocket.Connect ("127.0.0.1", 8888);
				serverStream = clientSocket.GetStream ();

				foreach (GameObject g in GameObject.FindGameObjectsWithTag("SpineMid")) {
						tposeSpineMid = g.transform.rotation;
				}
		
				foreach (GameObject g in GameObject.FindGameObjectsWithTag("Neck")) {
						tposeNeck = g.transform.rotation;
				}
		
				foreach (GameObject g in GameObject.FindGameObjectsWithTag("Head")) {
						tposeHead = g.transform.rotation;
				}
		
				foreach (GameObject g in GameObject.FindGameObjectsWithTag("LeftShoulder")) {
						tposeLeftShoulder = g.transform.rotation;
				}
		
				foreach (GameObject g in GameObject.FindGameObjectsWithTag("LeftElbow")) {
						tposeLeftElbow = g.transform.rotation;
				}
		
				foreach (GameObject g in GameObject.FindGameObjectsWithTag("LeftWrist")) {
						tposeLeftWrist = g.transform.rotation;
				}
		
				foreach (GameObject g in GameObject.FindGameObjectsWithTag("RightShoulder")) {
						tposeRightShoulder = g.transform.rotation;
				}
		
				foreach (GameObject g in GameObject.FindGameObjectsWithTag("RightWrist")) {
						tposeRightWrist = g.transform.rotation;
				}
				foreach (GameObject g in GameObject.FindGameObjectsWithTag("RightElbow")) {
						tposeRightElbow = g.transform.rotation;
				}
		
		}
	
	
		// Update is called once per frame
		void Update ()
		{
				if (!serverStream.DataAvailable) {
						// No new data
						return;
				}
				byte[] skeletonBuffer = null;
				// Iterate until I get the most recent data
				while (serverStream.DataAvailable) {
						byte[] lengthBuffer = new byte[4];
						serverStream.Read (lengthBuffer, 0, 4);
						int length = BitConverter.ToInt32 (lengthBuffer, 0);
						skeletonBuffer = new byte[length];
						serverStream.Read (skeletonBuffer, 0, length);
				}
				string returndata = System.Text.Encoding.ASCII.GetString (skeletonBuffer);

				Dictionary<int, string> dict = readTextFile (returndata);
				try {
						foreach (GameObject g in GameObject.FindGameObjectsWithTag("SpineMid")) {
								setRotation (g, dict [(int)JointType.SpineMid], tposeSpineMid);
						}
			
						foreach (GameObject g in GameObject.FindGameObjectsWithTag("Neck")) {
								setRotation (g, dict [(int)JointType.Neck], tposeNeck);
						}
			
						foreach (GameObject g in GameObject.FindGameObjectsWithTag("Head")) {
								setRotation (g, dict [(int)JointType.Head], tposeHead);
						}
			
						foreach (GameObject g in GameObject.FindGameObjectsWithTag("LeftShoulder")) {
								setRotation (g, dict [(int)JointType.ShoulderLeft], tposeLeftShoulder);
						}
			
						foreach (GameObject g in GameObject.FindGameObjectsWithTag("LeftElbow")) {
								setRotation (g, dict [(int)JointType.ElbowLeft], tposeLeftElbow);
						}
			
						foreach (GameObject g in GameObject.FindGameObjectsWithTag("LeftWrist")) {
								setRotation (g, dict [(int)JointType.WristLeft], tposeLeftWrist);
						}
			
						foreach (GameObject g in GameObject.FindGameObjectsWithTag("RightShoulder")) {
								setRotation (g, dict [(int)JointType.ShoulderRight], tposeRightShoulder);
						}
			
						foreach (GameObject g in GameObject.FindGameObjectsWithTag("RightElbow")) {
								setRotation (g, dict [(int)JointType.ElbowRight], tposeRightElbow);
						}
			
						foreach (GameObject g in GameObject.FindGameObjectsWithTag("RightWrist")) {
								setRotation (g, dict [(int)JointType.WristRight], tposeRightWrist);
						}
			
				} catch (IOException) {
						//Ignore
				}
		}
	
		// TPOSe unity Quaternion
		//		void setRotation (GameObject obj, string val, Quaternion startRotation, Vector3 startChange)
		void setRotation (GameObject obj, string val, Quaternion startRotation)
		{
				string[] quaternions = val.Split ('#');
				Quaternion start = getQuaternion (quaternions [0]); // TPOSE Kinect Quaternion
				Quaternion current = getQuaternion (quaternions [1]); // CURRENT Kinesct Quaternion
				Quaternion inputRelative = Quaternion.Inverse (start) * current; // Relative rotation Kinect Quaternion
				//Quaternion inputRelative = current * Quaternion.Inverse(start);
		
				Quaternion relative = startRotation * inputRelative;
		
				obj.transform.rotation = Quaternion.Slerp (obj.transform.rotation, relative, Time.time * .1f);
		}
	
		Quaternion getQuaternion (string val)
		{
				string[] values = val.Split ('|');
				Quaternion quat = new Quaternion ();
				quat.x = float.Parse (values [1]);
				quat.y = float.Parse (values [0]);
				quat.z = float.Parse (values [2]);
				quat.w = float.Parse (values [3]);
				return quat;
		}
	
		// Parses the file and returns a structure of (int)id -> (string)"a|b|c|d" where a,b,c,d are floats from the quaternion
		Dictionary<int, string> readTextFile (string streamString)
		{
				Dictionary<int, string> dictionary = new Dictionary<int, string> ();

				Debug.Log (streamString);
				string[] stream = streamString.Split ('\n');
				//Debug.Log ("LINES: " + stream.Length);
					//	Ignoring empty line at the end :)
				for (int i = 0; i < stream.Length - 1; i++) {
						string inp_ln = stream [i];
						//Debug.Log ("PARSING: " + inp_ln);
						string keys = inp_ln.Split ('*') [0];
						string values = inp_ln.Split ('*') [1];
						dictionary.Add (int.Parse (keys), values);
				}
		
				return dictionary;
		}
	
		public enum JointType
		{
				AnkleLeft =	14,
				AnkleRight = 18,
				ElbowLeft = 5,
				ElbowRight = 9,
				FootLeft = 15,
				FootRight = 19,
				HandLeft = 7,
				HandRight = 11,
				HandTipLeft = 21,
				HandTipRight = 23,
				Head = 3,
				HipLeft = 12,
				HipRight = 16,
				KneeLeft = 13,
				KneeRight = 17,
				Neck = 2,
				ShoulderLeft = 4,
				ShoulderRight = 8,
				SpineBase = 0,
				SpineMid = 1,
				SpineShoulder = 20,
				ThumbLeft = 22,
				ThumbRight = 24,
				WristLeft = 6,
				WristRight = 10
	}
		;
}