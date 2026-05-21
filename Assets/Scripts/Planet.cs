using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(LineRenderer))]
public class Planet : MonoBehaviour
{
    [SerializeField] private GameObject sun;
    [SerializeField] private float speed = 0.1f;
    private float angle;
    private float sunX;
    private float sunZ;
    private float orbitRadius;
    float semiMajorAxis;
    [SerializeField] private float eccentricity = 0.0167f;
    private Rigidbody rb;   
    private float speedY = 0.8f;
    private float torque = 0.02f;
    
    [SerializeField] private LineRenderer orbitLine;
    [SerializeField] private float orbitLineLifetime = 60f;
    [SerializeField] private float minPointDistance = 0.1f;
    private readonly List<Vector3> orbitPoints = new List<Vector3>();
    private readonly List<float> orbitPointTimes = new List<float>();
    private Vector3 lastOrbitPoint;

    private void Start()
    {
        //eccentricity *= 2;
        orbitLine = GetComponent<LineRenderer>();
        rb = GetComponent<Rigidbody>();
        if (sun == null) return;
        angle = speed;
        orbitRadius = Vector3.Distance(transform.position, sun.transform.position);
        semiMajorAxis = orbitRadius / (1f + eccentricity);
        StartCoroutine(RotateAroundSun());
    }

    private void Update()
    {
        rb.AddTorque(transform.up * (torque * speedY)); 
    }

    private IEnumerator RotateAroundSun()
    {
        if (gameObject.name != "Sole")
        {
            while (true)
            {
                sunX = sun.transform.position.x;
                sunZ = sun.transform.position.z;
            
                float semiLatusRectum = semiMajorAxis * (1f - eccentricity * eccentricity);

                angle += speed * Time.deltaTime;
                if (angle >= Mathf.PI * 2f)
                {
                    angle -= Mathf.PI * 2f;
                }

                float radius = semiLatusRectum / (1f + eccentricity * Mathf.Cos(angle));

                float newX = sun.transform.position.x + Mathf.Cos(angle) * radius;
                float newZ = sun.transform.position.z + Mathf.Sin(angle) * radius;
            
                Vector3 newPosition = new Vector3(newX, transform.position.y, newZ);
                transform.position = newPosition;
                UpdateOrbitLine(newPosition);
                yield return null;
            }
        }
        while (true)
        {
            Vector3 newSunPos = new Vector3(transform.position.x, transform.position.y, transform.position.z + 0.025f);
            transform.position = newSunPos;
            UpdateOrbitLine(newSunPos);
            yield return null;
        }
        
    }

    private void UpdateOrbitLine(Vector3 newPosition)
    {
        if (!orbitLine) return;

        if (orbitPoints.Count == 0 ||
        (orbitPoints[orbitPoints.Count - 1] - newPosition).sqrMagnitude >= minPointDistance * minPointDistance)
        {
            orbitPoints.Add(newPosition);
            orbitPointTimes.Add(Time.time);
        }

        while (orbitPointTimes.Count > 0 && Time.time - orbitPointTimes[0] > orbitLineLifetime)
        {
            orbitPoints.RemoveAt(0);
            orbitPointTimes.RemoveAt(0);
        }

        orbitLine.positionCount = orbitPoints.Count;
        orbitLine.SetPositions(orbitPoints.ToArray());
    }
}
