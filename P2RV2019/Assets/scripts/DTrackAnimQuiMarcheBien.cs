using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DTrackAnimQuiMarcheBien : MonoBehaviour
{

    private List<Transform> bones = new List<Transform>(); // liste des bones possédant le script DTrackPose
    private List<DTrackPose> bonesParent = new List<DTrackPose>();
    private List<Vector3> positionDtrack = new List<Vector3>();
    private List<Quaternion> rotationDtrack = new List<Quaternion>();
    private List<Vector3> positionOffset = new List<Vector3>();
    private List<Quaternion> rotationOffset = new List<Quaternion>();
    private float eps = 0.0001f;

    //Wait for a number of frame to make sure DTrack can send Data before we initialize the movement 
    [SerializeField]
    private int waitForFrame = 10;




    // Update is called once per frame
    void LateUpdate()
    {
        if (Time.frameCount == waitForFrame)    //A la 10e frame, on récupère les positions et orientations initiales du mannequin dans Unity, et les données brutes correspondantes fournies par Dtrack
        {
            initListes(this.transform);     //On parcourt récursivement la hiérarchie pour compléter, dans le bon ordre, les listes bones, positionDtrack et rotationDtrack
            calculOffset(bones, positionDtrack, rotationDtrack);    //A partir des listes remplies ci-dessus, on calcule les offsets relatifs, en position et en rotation, que l'on stocke dans les liste positionOffset et rotationOffset
            for (int i = 0; i < bones.Count; i++)
            {
                bonesParent.Add(getDtrackparent(bones[i].gameObject));
            }
        }
        if (Time.frameCount > waitForFrame)  //A chaque nouvelle frame, on actualise la position de chaque bone, en respectant l'ordre de la hiérarchie (ordre de remplissage des listes)
        {
            for (int i = 0; i < bones.Count; i++)
            {
                SetTransform(bones[i], rotationOffset[i], positionDtrack[i]);
            }
        }
    }


    private void initListes(Transform parent)
    {
        if (parent.childCount == 0) //Si l'objet n'a pas d'enfant, il n'y a pas de nouveau bone à traiter
        {
            return;
        }

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform currentChild = parent.GetChild(i);
            DTrackPose pose = currentChild.GetComponent<DTrackPose>();
            if (pose)
            {
                bones.Add(currentChild);
                rotationDtrack.Add(pose.body_desc.orientation);
                positionDtrack.Add(pose.body_desc.position);
            }

            initListes(currentChild);
        }
    }

    private DTrackPose getDtrackparent(GameObject child)
    {
        Transform parent = child.GetComponentInParent<Transform>();
        if (parent)
        {
            DTrackPose pose = parent.gameObject.GetComponent<DTrackPose>();
            if (pose)
            {
                return pose;
            }
            else
            {
                pose = getDtrackparent(parent.gameObject);
                if (pose.id == -1)
                {
                    if (child.gameObject.GetComponent<DTrackPose>())
                    {
                        pose = child.gameObject.GetComponent<DTrackPose>();
                    }
                }
                return pose;
            }
        }
        else
        {
            DTrackPose pose = child.gameObject.GetComponent<DTrackPose>();
            if (pose)
            {
                return pose;
            }
            else
            {
                pose = new DTrackPose();
                pose.id = -1;
                return pose;
            }
        }
    }

    private void calculOffset(List<Transform> bonesList, List<Vector3> positions, List<Quaternion> orientations)
    {
        for (int i = 0; i < bonesList.Count; i++)
        {
            //Calcul de l'offset en position
            //on met les coordonnées recues par Dtrack en locale
            Vector3 posDtrackRelatif = positions[i] - bonesParent[i].body_desc.position;

            Vector3 Poffset = posDtrackRelatif - bonesList[i].localPosition;
            /*
            //passage en local
            DTrackPose pose = bonesList[i].GetComponentInParent<DTrackPose>();
            Poffset = pose.p * Poffset;
            */
            positionOffset.Add(Poffset);

            //Calcul de l'offset en rotation
            Quaternion offset = Quaternion.Inverse(bonesList[i].rotation) * orientations[i];
            /*
            //passage en local
            offset = pose.inv_p.ExtractQuaternion() * offset * pose.p.ExtractQuaternion();
            */
            rotationOffset.Add(offset);
        }
    }

    //Fonction SetTransform fournie par le groupe P2RV 2018
    private void SetTransform(Transform bone, Quaternion Roffset, Vector3 Poffset)
    {
        // On récupère le composant DTrackPose
        DTrackPose pose = bone.GetComponent<DTrackPose>();
        // On applique le changement de repère spécifique à la pièce traquée
        Matrix4x4 newRotationMatrix = pose.inv_p * pose.body_desc.rotationMatrix * pose.p;
        // nouveau Quaternion
        Quaternion newOrientation = newRotationMatrix.ExtractQuaternion();
        //Quaternion quat = Quaternion.Inverse (Roffset) * pose.body_desc.orientation;
        Quaternion quat = Quaternion.Inverse(Roffset) * newOrientation;
        //bone.localRotation = Rstandard*quat;
        if (!(Mathf.Abs(quat.x) < eps && Mathf.Abs(quat.y) < eps && Mathf.Abs(quat.z) < eps && Mathf.Abs(quat.w) < eps))
        {
            bone.localRotation = bone.localRotation * Quaternion.Inverse(bone.parent.rotation) * quat;
            //bone.localRotation = bone.localRotation * quat;
            //bone.localRotation = Rstandard * quat;
            bone.localPosition = bone.position - bone.GetComponentInParent<Transform>().position - Poffset;
        }
    }
}
