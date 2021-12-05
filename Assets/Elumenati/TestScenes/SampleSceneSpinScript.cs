using UnityEngine;

public class SampleSceneSpinScript : MonoBehaviour {

    // Use this for initialization
    private void Start() {
    }

    public float speed = .1f;
    public float radius = 1.5f;

    public Vector3 rotateXYZ = Vector3.zero;

    // Update is called once per frame
    private void Update() {
        transform.localPosition = new Vector3(radius * Mathf.Sin(Time.time * speed), 0, radius * Mathf.Cos(Time.time * speed));

        if (rotateXYZ.sqrMagnitude <= 0.01) {
            transform.LookAt(Vector3.zero);
        } else {
            transform.localEulerAngles = rotateXYZ * Time.time;
        }
    }
}