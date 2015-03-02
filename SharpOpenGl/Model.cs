using OpenTK;

namespace SharpOpenGl
{
    class Model
    {
        public int VertexCount { get; set; }
        public string ModelName { get; set; }
        public string ModelTextureList { get; set; }
        public string ModelMaterialList { get; set; }
        public Vector3 ModelOrientationVector { get; set; }
        public Vector3 ModelPositionVector { get; set; }

        public Vector3[] VertexList;
        public Vector3[] NormalList;

        public Model()
        {
            ModelOrientationVector = new Vector3(0,0,0);
            ModelPositionVector = new Vector3(0, 0, 0);
        }

        public void SetLocation(float x, float y, float z, float xr, float yr, float zr)
        {
            ModelPositionVector = new Vector3(x,y,z);
            ModelOrientationVector = new Vector3(xr, yr, zr);
        }

        public void AddX(float x)
        {
            ModelPositionVector = new Vector3(ModelPositionVector.X + x, ModelPositionVector.Y, ModelPositionVector.Z);
        }

        public void AddY(float y)
        {
            ModelPositionVector = new Vector3(ModelPositionVector.X , ModelPositionVector.Y+y, ModelPositionVector.Z);
        }

        public void AddZ(float z)
        {
            ModelPositionVector = new Vector3(ModelPositionVector.X , ModelPositionVector.Y, ModelPositionVector.Z+z);
        }

        public void AddXRot(float x)
        {
            ModelOrientationVector = new Vector3(ModelOrientationVector.X+x, ModelOrientationVector.Y, ModelOrientationVector.Z);
        }

        public void AddYRot( float y)
        {
            ModelOrientationVector = new Vector3(ModelOrientationVector.X, ModelOrientationVector.Y+y, ModelOrientationVector.Z);
        }

        public void AddZRot(float z)
        {
            ModelOrientationVector = new Vector3(ModelOrientationVector.X, ModelOrientationVector.Y, ModelOrientationVector.Z + z);
        }
    }
}
