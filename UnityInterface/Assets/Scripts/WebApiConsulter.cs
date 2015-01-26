using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;

public class WebApiConsulter : MonoBehaviour
{

		private string filePath = @"C:\Users\sheetah\Documents\RagdollTest\Assets\Input\skeleton.txt";
		private Quaternion tposeSpineMid;
		private Quaternion tposeNeck;
		private Quaternion tposeHead;
		private Quaternion tposeLeftShoulder;
		private Quaternion tposeLeftElbow;
		private Quaternion tposeLeftWrist;
		private Quaternion tposeRightShoulder;
		private Quaternion tposeRightElbow;
		private Quaternion tposeRightWrist;

		void Start ()
		{
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
				try {
						Dictionary<int, string> dict = readTextFile (filePath);
						foreach (GameObject g in GameObject.FindGameObjectsWithTag("SpineMid")) {
								setRotation (g, dict [(int)JointType.SpineMid], tposeSpineMid);
								//								setRotation (g, dict [(int)JointType.SpineMid], tposeSpineMid, new Vector3 (0, 0, 357.8513f));
								//						StartCoroutine (WaitForRequest (new WWW (basicURL + JointType.SpineMid), g));
						}
			
						foreach (GameObject g in GameObject.FindGameObjectsWithTag("Neck")) {
								setRotation (g, dict [(int)JointType.Neck], tposeNeck);
								//								setRotation (g, dict [(int)JointType.Neck], tposeNeck, new Vector3 (0, 0, 325.2086f));
								//StartCoroutine (WaitForRequest (new WWW (basicURL + JointType.Neck), g));
						}
			
						foreach (GameObject g in GameObject.FindGameObjectsWithTag("Head")) {
								setRotation (g, dict [(int)JointType.Head], tposeHead);
								//setRotation (g, dict [(int)JointType.Head], tposeHead, new Vector3 (0, 0, 0));
								//			StartCoroutine (WaitForRequest (new WWW (basicURL + JointType.Head), g));
						}
			
						foreach (GameObject g in GameObject.FindGameObjectsWithTag("LeftShoulder")) {
//								setRotation (g, dict [(int)JointType.ShoulderLeft], tposeLeftShoulder, new Vector3 (225f, -225f, -270f));
								setRotation (g, dict [(int)JointType.ShoulderLeft], tposeLeftShoulder);
								//				StartCoroutine (WaitForRequest (new WWW (basicURL + JointType.ShoulderLeft), g));
						}
			
						foreach (GameObject g in GameObject.FindGameObjectsWithTag("LeftElbow")) {
								setRotation (g, dict [(int)JointType.ElbowLeft], tposeLeftElbow);
								//setRotation (g, dict [(int)JointType.ElbowLeft], tposeLeftElbow, new Vector3 (-11.21715f, -337.8051f, 252.7622f));
								//StartCoroutine (WaitForRequest (new WWW (basicURL + JointType.ElbowLeft), g));
						}
			
						foreach (GameObject g in GameObject.FindGameObjectsWithTag("LeftWrist")) {
								setRotation (g, dict [(int)JointType.WristLeft], tposeLeftWrist);
								//								setRotation (g, dict [(int)JointType.WristLeft], tposeLeftWrist, new Vector3 (359.3031f, -358.643f, -251.9507f));
								//StartCoroutine (WaitForRequest (new WWW (basicURL + JointType.WristLeft), g));
						}

						foreach (GameObject g in GameObject.FindGameObjectsWithTag("RightShoulder")) {
								setRotation (g, dict [(int)JointType.ShoulderRight], tposeRightShoulder);
								//								setRotation (g, dict [(int)JointType.ShoulderRight], tposeRightShoulder, new Vector3 (40, -130, -90));
								//				StartCoroutine (WaitForRequest (new WWW (basicURL + JointType.ShoulderLeft), g));
						}
			
						foreach (GameObject g in GameObject.FindGameObjectsWithTag("RightElbow")) {
								setRotation (g, dict [(int)JointType.ElbowRight], tposeRightElbow);
								//								setRotation (g, dict [(int)JointType.ElbowRight], tposeRightElbow, new Vector3 (-221.9063f, -77.49442f, 5.789215f));
								//StartCoroutine (WaitForRequest (new WWW (basicURL + JointType.ElbowLeft), g));
						}
			
						foreach (GameObject g in GameObject.FindGameObjectsWithTag("RightWrist")) {
								setRotation (g, dict [(int)JointType.WristRight], tposeRightWrist);
//								setRotation (g, dict [(int)JointType.WristRight], tposeRightWrist, new Vector3 (180, 357.461f, 0));
								//StartCoroutine (WaitForRequest (new WWW (basicURL + JointType.WristLeft), g));
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
//				Quaternion current = getQuaternion (quaternions [0]);
				Quaternion current = getQuaternion (quaternions [1]); // CURRENT Kinesct Quaternion
				Quaternion inputRelative = Quaternion.Inverse (start) * current; // Relative rotation Kinect Quaternion
//				Vector3 euler3 = inputRelative.eulerAngles;
//				Debug.Log ("(" + (euler3.x) + ", " + (euler3.y) + ", " + (euler3.z) + ")");

//				Quaternion relative = Quaternion.Inverse (startRotation) * inputRelative;
				Quaternion relative = startRotation * inputRelative;
//				Debug.Log("Input relative: (" + (relative.x - startRotation.x) + ", " + (relative.y - startRotation.y) + ", " + (relative.z - startRotation.z) + ", " + (relative.w - startRotation.w) + ")");
//				Vector3 euler = relative.eulerAngles;
//				Vector3 startE = startRotation.eulerAngles;
//				Debug.Log ("(" + (startE.x - euler.x) + ", " + (startE.y - euler.y) + ", " + (startE.z - euler.z) + ")");

//				obj.transform.localRotation = relative;
				obj.transform.rotation = relative;
//				obj.transform.localEulerAngles = relative.eulerAngles - startChange;
//		Debug.Log ("(" + (obj.transform.localEulerAngles.x) + ", " + (obj.transform.localEulerAngles.y) + ", " + (obj.transform.localEulerAngles.z) + ")");
		}

		Quaternion getQuaternion (string val)
		{
				string[] values = val.Split ('|');
				Quaternion quat = new Quaternion ();
				quat.x = float.Parse (values [0]);
				quat.y = float.Parse (values [1]);
				quat.z = float.Parse (values [2]);
				quat.w = float.Parse (values [3]);
				return quat;
		}
	
		// Parses the file and returns a structure of (int)id -> (string)"a|b|c|d" where a,b,c,d are floats from the quaternion
		Dictionary<int, string> readTextFile (string file_path)
		{
				Dictionary<int, string> dictionary = new Dictionary<int, string> ();
				//Hashtable ht = new Hashtable ();
				StreamReader inp_stm = new StreamReader (file_path);
		
				while (!inp_stm.EndOfStream) {
						string inp_ln = inp_stm.ReadLine ();
						string keys = inp_ln.Split ('*') [0];
						string values = inp_ln.Split ('*') [1];
						dictionary.Add (int.Parse (keys), values);
				}
		
				inp_stm.Close ();
				return dictionary;
		}

/*	
		IEnumerator WaitForRequest (WWW www, GameObject obj)
		{
				yield return www;
		
				// check for errors
				if (www.error == null) {
						string ans = www.text;
						string[] values = ans.Split ('|');
						Quaternion quat = new Quaternion ();
						quat.x = float.Parse (values [0]);
						quat.y = float.Parse (values [1]);
						quat.z = float.Parse (values [2]);
						quat.w = float.Parse (values [3]);
						obj.transform.rotation = quat;
						Debug.Log ("WWW Error: " + www.text);
				} else {
						Debug.Log ("WWW Error: " + www.error);
				}    
		}
*/

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
