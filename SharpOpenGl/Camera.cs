using System;
using OpenTK.Graphics.OpenGL;
using OpenTK;

namespace SharpOpenGl
{
    class Camera
    {
        public Vector3 CameraPosition{get;set;}
        public Vector3 xLocation{get;set;}
        public Vector3 yLocation{get;set;}
        public Vector3 zLocation{get;set;}
        public float xRotation{get;set;}
        public float yRotation{get;set;}
        public float zRotation{get;set;}

        /// <summary>
        /// create the new camera, set the rotation angles to nothing, the z location into the screen, the x location to the right, and the y location up.
        /// </summary>
        public Camera()
        {
            CameraPosition = new Vector3(0.0f, 0.0f, 0.0f);
            xLocation = new Vector3(0.0f, 0.0f, -1.0f);
            yLocation = new Vector3(1.0f, 0.0f, 0.0f);
            zLocation = new Vector3(0.0f, 1.0f, 0.0f);

            xRotation = 0.0f;
            yRotation = 0.0f;
            zRotation = 0.0f;
        }

        /// <summary>
        /// Fires off the camera position on the screen.
        /// </summary>
        public void AdjustCamera()
        {
            Vector3 focalPoint = CameraPosition+zLocation;

	        Matrix4 lookat = Matrix4.LookAt(CameraPosition.X, CameraPosition.Y, CameraPosition.Z, focalPoint.X, focalPoint.Y, focalPoint.Z, yLocation.X, yLocation.Y, yLocation.Z);
            GL.MatrixMode(MatrixMode.Modelview);
            GL.LoadMatrix(ref lookat); 
        }

        /// <summary>
        /// Move the camera by a vector projection.
        /// </summary>
        /// <param name="projection"></param>
        public void MoveCamera (Vector3 projection)
        {
	        CameraPosition = CameraPosition + projection;
        }

        /// <summary>
        /// Rotate along the x axis the provided angle
        /// </summary>
        /// <param name="angle"></param>
        public void RotateX(float angle)
        {
	        xRotation += angle;
            zLocation = CleanVector(zLocation*(float)Math.Cos(DegreeToRadian(angle)) + yLocation*(float)Math.Sin(DegreeToRadian(angle)));
            yLocation = Vector3.Cross(zLocation,xLocation) * -1;
        }

        /// <summary>
        /// /// Rotate along the y axis the provided angle
        /// </summary>
        /// <param name="angle"></param>
        public void RotateY(float angle)
        {
	        yRotation += angle;
	        zLocation = CleanVector(zLocation*(float)Math.Cos(DegreeToRadian(angle)) - xLocation*(float)Math.Sin(DegreeToRadian(angle)));
            xLocation = Vector3.Cross(zLocation,yLocation);
        }

        /// <summary>
        /// Rotate along the z axis the provided angle
        /// </summary>
        /// <param name="angle"></param>
        public void RotateZ(float angle)
        {
	        zRotation += angle;
            xLocation = CleanVector(xLocation*(float)Math.Cos(DegreeToRadian(angle))+yLocation*(float)Math.Sin(DegreeToRadian(angle)));
	        yLocation = Vector3.Cross(zLocation, xLocation) * -1;
        }

        /// <summary>
        /// Move the camera along the x axis.
        /// </summary>
        /// <param name="distance"></param>
        public void MoveXAxis (float distance)
        {
	        CameraPosition = CameraPosition + (xLocation * distance);
        }

        /// <summary>
        /// Move the camera along the y axis.
        /// </summary>
        /// <param name="distance"></param>
        public void MoveYAxis(float distance)
        {
	        CameraPosition= CameraPosition + (yLocation * distance);
        }

        /// <summary>
        /// Move the camera along the z axis.
        /// </summary>
        /// <param name="distance"></param>
        public void MoveZAxis(float distance)
        {
	        CameraPosition= CameraPosition + (zLocation * -distance);
        }

        /// <summary>
        /// Gimme a degree, I'll give you a radian. (uses a low interpretation of radian = PI / 180.
        /// </summary>
        /// <param name="todo"></param>
        /// <returns></returns>
        public float DegreeToRadian(float toConvert)
        {
            return toConvert*(3.14159f/180.0f);
        }

        /// <summary>
        /// The points get messy, here's some cleanup.  Kamera needs this.
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        public Vector3 CleanVector(Vector3 v)
        {
	        Vector3 result;
	        float l = (float)(Math.Sqrt((v.X*v.X)+(v.Y*v.Y)+(v.Z*v.Z)));
	       
	        result.X = v.X / l;
	        result.Y = v.Y / l;
	        result.Z = v.Z / l;
	        return result;
        }
    }
}