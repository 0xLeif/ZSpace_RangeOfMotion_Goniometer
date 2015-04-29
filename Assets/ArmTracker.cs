using UnityEngine;
using System.Collections;

public class ArmTracker : MonoBehaviour {
    public ZSStylusSelector stylus;
    public GameObject arm1, arm2, stylusVirtualNotTheRealTipTip, stylusBeam, stylusBeamEnd;
    // Use this for initialization
    void Start() {
    }

    // Update is called once per frame
    void Update() {
        if (stylus.GetButton(1))
            rotateArm(arm2);
        else if (stylus.GetButtonUp(1))
            setVisibilityOfStylus(true);
    }

    Quaternion updateRotation() {
        Vector3 stylusWorldPosistion = Camera.main.ScreenToWorldPoint(stylus.transform.position);
        Vector3 relativePos = stylusVirtualNotTheRealTipTip.transform.position - transform.position;
        return Quaternion.LookRotation(relativePos);
    }

    void rotateArm(GameObject go) {
        setVisibilityOfStylus(false);
        go.transform.localRotation = Quaternion.Euler(0, updateRotation().eulerAngles.y, 0);
        go.transform.Rotate(0, -90, 0);
    }

    void setVisibilityOfStylus(bool b) {
        stylusVirtualNotTheRealTipTip.GetComponent<MeshRenderer>().enabled = b;
        stylusBeam.GetComponent<MeshRenderer>().enabled = b;
        stylusBeamEnd.GetComponent<MeshRenderer>().enabled = b;
    }

}
