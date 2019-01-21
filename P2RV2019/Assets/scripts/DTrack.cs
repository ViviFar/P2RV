using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.Net.Sockets;

public class DTrack : MonoBehaviour
{
	public struct BodyDesc
	{
		public int id;
		public float quality;
		public Vector3 position;
		public Quaternion orientation;
		public Matrix4x4 rotationMatrix;
	}

	public int port = 5000;

	UdpClient udpClient;

	Dictionary<int, BodyDesc> bodies;

	Matrix4x4 p, inv_p;


	// Use this for initialization
	void Start()
	{
		udpClient = new UdpClient(port);

		p = Matrix4x4.identity;
		/*p.m22 = 0.0f;
		p.m00 = -1.0f;
		p.m12 = -1.0f;
		p.m21 = 1.0f;
		p.m11 = 0.0f;*/
		inv_p = p.inverse;
	}

	// Update is called once per frame
	void Update()
	{
		while(udpClient.Available > 0)
		{
			IPEndPoint ep = null;

			byte[] bytes;

			bytes = udpClient.Receive(ref ep);

			if(bytes != null)
			{
				string s = System.Text.Encoding.Default.GetString(bytes);

				if(!string.IsNullOrEmpty(s))
				{
					//print(s);

					char[] separators = { ' ', '[', ']', '\r', '\n' };

					string[] toto = s.Split(separators);

					int index_6d = -1;

					for(int i = 0; i < toto.Length; i++)
					{
						if(toto[i] == "6d")
						{
							index_6d = i;

							break;
						}
					}

					int count = int.Parse(toto[index_6d + 1]);

					bodies = new Dictionary<int, BodyDesc>();

					int current_index = index_6d + 2;

					for(int i = 0; i < count; i++)
					{
						BodyDesc body = new BodyDesc();

						current_index++;

						body.id = int.Parse(toto[current_index]);

						current_index++;


                        toto[current_index] = toto[current_index].Replace('.', ',');
						body.quality = float.Parse(toto[current_index]);

						current_index += 2;

						Matrix4x4 m = Matrix4x4.identity;

                        toto[current_index] = toto[current_index].Replace('.', ',');
                        m.m03 = 0.001f * float.Parse(toto[current_index]);

						current_index++;

                        toto[current_index] = toto[current_index].Replace('.', ',');
                        m.m13 = 0.001f * float.Parse(toto[current_index]);

						current_index++;

                        toto[current_index] = toto[current_index].Replace('.', ',');
                        m.m23 = 0.001f * float.Parse(toto[current_index]);

						current_index += 5;


                        toto[current_index] = toto[current_index].Replace('.', ',');
                        m.m00 = float.Parse(toto[current_index]);
						current_index++;
                        toto[current_index] = toto[current_index].Replace('.', ',');
                        m.m10 = float.Parse(toto[current_index]);
						current_index++;
                        toto[current_index] = toto[current_index].Replace('.', ',');
                        m.m20 = float.Parse(toto[current_index]);
						current_index++;
                        toto[current_index] = toto[current_index].Replace('.', ',');
                        m.m01 = float.Parse(toto[current_index]);
						current_index++;
                        toto[current_index] = toto[current_index].Replace('.', ',');
                        m.m11 = float.Parse(toto[current_index]);
						current_index++;
                        toto[current_index] = toto[current_index].Replace('.', ',');
                        m.m21 = float.Parse(toto[current_index]);
						current_index++;
                        toto[current_index] = toto[current_index].Replace('.', ',');
                        m.m02 = float.Parse(toto[current_index]);
						current_index++;
                        toto[current_index] = toto[current_index].Replace('.', ',');
                        m.m12 = float.Parse(toto[current_index]);
						current_index++;
                        toto[current_index] = toto[current_index].Replace('.', ',');
                        m.m22 = float.Parse(toto[current_index]);

						Matrix4x4 a = p * m * inv_p; // on passe la matrice du repère de la salle au repère de Unity

						body.position = a.ExtractPosition();
						/*float temp = body.position.y;
						body.position.y = -body.position.z;
						body.position.z = temp;*/
						body.orientation = a.ExtractQuaternion();

						body.rotationMatrix = a;

						bodies[body.id] = body;

                        Debug.Log(body.id);

						current_index += 2;
					}
				}
			}
		}
	}

	public bool GetBody(int id, out BodyDesc body)
	{
		body = new BodyDesc();

		if(bodies != null)
		{
			return bodies.TryGetValue(id, out body);
		}
		else
		{
			return false;
		}
	}
}
