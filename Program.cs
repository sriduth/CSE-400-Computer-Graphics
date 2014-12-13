using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Media;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading;
using System.Diagnostics;

using OpenTK;
using OpenTK.Graphics.OpenGL;
using OpenTK.Graphics;

/*
 * Required for playing audio
 */
using WMPLib;

namespace Shader
{
    class ShaderUnit
    {
        /*
         *  ProgramID holds address of GPU program.
         *  VShader and FShader are the vertex and fragment shader id's on the GPU.
         */
        public int ProgramID = -1;
        public int VShaderID = -1;
        public int FShaderID = -1;
        public int AttributeCount = 0;
        public int UniformCount = 0;

        public Dictionary<String, AttributeInfo> Attributes = new Dictionary<string, AttributeInfo>();
        public Dictionary<String, UniformInfo> Uniforms = new Dictionary<string, UniformInfo>();
        public Dictionary<String, uint> Buffers = new Dictionary<string, uint>();

        /*
         * Init the program, get program Id.Id holds the address of GPU code.
         */
        public ShaderUnit()
        {
            ProgramID = GL.CreateProgram();          
        }

        /*
         *  Adding more functionality to our constructor.
         */
        public ShaderUnit(String vshader, String fshader, bool fromFile = true)
        {
            // Basic step - Shake hands with the GPU.
            ProgramID = GL.CreateProgram();

            // Load Shaders from file - vs.glsl / fs.glsl
            if (fromFile)
            {
                LoadShaderFromFile(vshader, ShaderType.VertexShader);
                LoadShaderFromFile(fshader, ShaderType.FragmentShader);
            }

            // Load the shader given its unique name. 
            else
            {
                LoadShaderFromString(vshader, ShaderType.VertexShader);
                LoadShaderFromString(fshader, ShaderType.FragmentShader);
            }

            /*
             * Link the shaders to our Program(ProgramID) and generate buffers to send 
             * instructions to the GPU.
             */
            Link();
            GenBuffers();
        }

        /*
         *  Load Vertex and Fragment shaders from : fs.glsl and vs.glsl.
         */
        private void LoadShader(String code, ShaderType type, out int address)
        {
            address = GL.CreateShader(type);
            GL.ShaderSource(address, code);
            GL.CompileShader(address);
            GL.AttachShader(ProgramID, address);    
            Console.WriteLine(GL.GetShaderInfoLog(address));
        }

        /*
         *  Presumable used to load shaders based on code?
         */
        public void LoadShaderFromString(String code, ShaderType type)
        {
            if (type == ShaderType.VertexShader)
                LoadShader(code, type, out VShaderID);

            else if (type == ShaderType.FragmentShader)
                LoadShader(code, type, out FShaderID);

        }

        /*
         * Load shaders for vs.glsl and fs.glsl.
         */
        public void LoadShaderFromFile(String filename, ShaderType type)
        {
            using (StreamReader sr = new StreamReader(filename))
            {
                if (type == ShaderType.VertexShader)
                    LoadShader(sr.ReadToEnd(), type, out VShaderID);

                else if (type == ShaderType.FragmentShader)
                    LoadShader(sr.ReadToEnd(), type, out FShaderID);
            }
        }

        /*
         *  Linking the shaders 
         */
        public void Link()
        {
            GL.LinkProgram(ProgramID);

            Console.WriteLine(GL.GetProgramInfoLog(ProgramID));

            GL.GetProgram(ProgramID, ProgramParameter.ActiveAttributes, out AttributeCount);
            GL.GetProgram(ProgramID, ProgramParameter.ActiveUniforms, out UniformCount);

            for (int i = 0; i < AttributeCount; i++)
            {
                AttributeInfo info = new AttributeInfo();
                int length = 0;

                StringBuilder name = new StringBuilder();

                GL.GetActiveAttrib(ProgramID, i, 256, out length, out info.size, out info.type, name);

                info.name = name.ToString();
                info.address = GL.GetAttribLocation(ProgramID, info.name);
                Attributes.Add(name.ToString(), info);
            }

            for (int i = 0; i < UniformCount; i++)
            {
                UniformInfo info = new UniformInfo();
                int length = 0;

                StringBuilder name = new StringBuilder();

                GL.GetActiveUniform(ProgramID, i, 256, out length, out info.size, out info.type, name);

                info.name = name.ToString();
                Uniforms.Add(name.ToString(), info);
                info.address = GL.GetUniformLocation(ProgramID, info.name);
            }
        }

        /*
         *  Generates Buffer objects for our shaders. We send instructions to the shader units by pushing info into the buffers.
         */
        public void GenBuffers()
        {
            for (int i = 0; i < Attributes.Count; i++)
            {
                uint buffer = 0;
                GL.GenBuffers(1, out buffer);

                Buffers.Add(Attributes.Values.ElementAt(i).name, buffer);
            }

            for (int i = 0; i < Uniforms.Count; i++)
            {
                uint buffer = 0;
                GL.GenBuffers(1, out buffer);

                Buffers.Add(Uniforms.Values.ElementAt(i).name, buffer);
            }
        }

        /*
         *  Enables the vertex attribute array.
         *   Replaces GL.EnableVertesAttribArray(); in OnRenderFrame();
         */
        public void EnableVertexAttribArrays()
        {
            for (int i = 0; i < Attributes.Count; i++)
                GL.EnableVertexAttribArray(Attributes.Values.ElementAt(i).address);
        }

        /*
         * Disables the vertex attribute array.
         * Replaces GL.DisableVertexAtibArray(); in OnRenderFrame();
         */
        public void DisableVertexAttribArrays()
        {
            for (int i = 0; i < Attributes.Count; i++)
                GL.DisableVertexAttribArray(Attributes.Values.ElementAt(i).address);
        }

        /*
         *  GetAttribute() GetUniform() and GetBuffer() all return what they are supposed to return,
         *  the references to the attributes of the shaders.
         */
        public int GetAttribute(string name)
        {
            if (Attributes.ContainsKey(name))
                return Attributes[name].address;
            else
                return -1;
        }

        public int GetUniform(string name)
        {
            if (Uniforms.ContainsKey(name))
                return Uniforms[name].address;
            else
                return -1;
        }

        public uint GetBuffer(string name)
        {
            if (Buffers.ContainsKey(name))
                return Buffers[name];
            else
                return 0;
        }
    }
    public class AttributeInfo
    {
        public String name = "";
        public int address = -1;
        public int size = 0;
        public ActiveAttribType type;
    }
    public class UniformInfo
    {
        public String name = "";
        public int address = -1;
        public int size = 0;
        public ActiveUniformType type;
    }


    class Camera
    {
        public Vector3 Position = Vector3.Zero;
        public Vector3 Orientation = new Vector3((float)Math.PI, 0f, 0f);
        public float MoveSpeed = 1f;
        public float MouseSensitivity = 0.01f;

        /*
         * Gets the Viewmatrix of the camera, relative to world view.
         */
        public Matrix4 GetViewMatrix()
        {
            Vector3 lookat = new Vector3();

            lookat.X = (float)(Math.Sin((float)Orientation.X) * Math.Cos((float)Orientation.Y));
            lookat.Y = (float)Math.Sin((float)Orientation.Y);
            lookat.Z = (float)(Math.Cos((float)Orientation.X) * Math.Cos((float)Orientation.Y));

            return Matrix4.LookAt(Position, Position + lookat, Vector3.UnitY);
        }

        /*
         * Camera moves relative to its own view.
         * Not the world.
         */
        public void Move(float x, float y, float z)
        {
            Vector3 offset = new Vector3();
            Vector3 forward = new Vector3((float)Math.Sin((float)Orientation.X), 0, (float)Math.Cos((float)Orientation.X));
            Vector3 right = new Vector3(-forward.Z, 0, forward.X);

            offset += x * right;
            offset += y * forward;
            offset.Y += z;

            offset.NormalizeFast();
            offset = Vector3.Multiply(offset, MoveSpeed);

            Position += offset;
        }

        /*
         * Rotates the camera.
         */
        public void AddRotation(float x, float y)
        {
            x = x * MouseSensitivity;
            y = y * MouseSensitivity;

            Orientation.X = (Orientation.X + x) % ((float)Math.PI * 2.0f);
            Orientation.Y = Math.Max(Math.Min(Orientation.Y + y, (float)Math.PI / 2.0f - 0.1f), (float)-Math.PI / 2.0f + 0.1f);
        }
    }
    public abstract class Volume
    {
        /*
         * Volume orientation
         */
        public Vector3 Position = Vector3.Zero;
        public Vector3 Rotation = Vector3.Zero;
        public Vector3 Scale = Vector3.One;

        public int VertCount;
        public int IndiceCount;
        public int ColorDataCount;
        public Matrix4 ModelMatrix = Matrix4.Identity;
        public Matrix4 ViewProjectionMatrix = Matrix4.Identity;
        public Matrix4 ModelViewProjectionMatrix = Matrix4.Identity;

        public abstract Vector3[] GetVerts();
        public abstract int[] GetIndices(int offset = 0);
        public abstract Vector3[] GetColorData();
        public abstract void CalculateModelMatrix();

        /*
         * Texture related Functions to be overridden in Child classes.
         */
        public bool IsTextured = false;
        public int TextureID;
        public int TextureCoordsCount;
        public abstract Vector2[] GetTextureCoords();
    }
    public class Tetra : Volume
    {
        /*
         * The vectors to the corners of the pyrramid.
         */
        private Vector3 PointApex;
        private Vector3 PointA;
        private Vector3 PointB;
        private Vector3 PointC;

        public Tetra(Vector3 apex, Vector3 a, Vector3 b, Vector3 c)
        {
            PointApex = apex;
            PointA = a;
            PointB = b;
            PointC = c;

            // Members of volume class
            VertCount = 4;
            IndiceCount = 12;
            ColorDataCount = 4;
        }

        /*
         * Cuts the large triangle into many small triangles.
         */
        public List<Tetra> Divide(int n = 0)
        {
            if (n == 0)
            {
                return new List<Tetra>(new Tetra[] { this });
            }
            else
            {

                Vector3 halfa = (PointApex + PointA) / 2.0f;
                Vector3 halfb = (PointApex + PointB) / 2.0f;
                Vector3 halfc = (PointApex + PointC) / 2.0f;

                // Calculate points half way between base points
                Vector3 halfab = (PointA + PointB) / 2.0f;
                Vector3 halfbc = (PointB + PointC) / 2.0f;
                Vector3 halfac = (PointA + PointC) / 2.0f;

                Tetra t1 = new Tetra(PointApex, halfa, halfb, halfc);
                Tetra t2 = new Tetra(halfa, PointA, halfab, halfac);
                Tetra t3 = new Tetra(halfb, halfab, PointB, halfbc);
                Tetra t4 = new Tetra(halfc, halfac, halfbc, PointC);

                List<Tetra> output = new List<Tetra>();

                output.AddRange(t1.Divide(n - 1));
                output.AddRange(t2.Divide(n - 1));
                output.AddRange(t3.Divide(n - 1));
                output.AddRange(t4.Divide(n - 1));

                return output;

            }
        }

        /*
         * Returns the vertices of the pyramid. Overrides the GetVerts() method of Volume class
         */
        public override Vector3[] GetVerts()
        {
            return new Vector3[] { PointApex, PointA, PointB, PointC };
        }

        /*
         * Overtides GetIndices() of Volume class.Return int[] of indices.
         */
        public override int[] GetIndices(int offset = 0)
        {
            int[] inds = new int[] { //bottom
                                1,3,2,
                                //other sides
                                0,1,2,
                                0,2,3,
                                0,3,1
             };

            if (offset != 0)
            {
                for (int i = 0; i < inds.Length; i++)
                {
                    inds[i] += offset;
                }
            }
            return inds;
        }

        /*
         * Mor overriden metods of Volume class.
         */
        public override Vector3[] GetColorData()
        {
            return new Vector3[] { new Vector3(1f, 0f, 0f), new Vector3(0f, 1f, 0f), new Vector3(0f, 0f, 1f), new Vector3(1f, 1f, 0f) };
        }

        public override void CalculateModelMatrix()
        {
            ModelMatrix = Matrix4.Scale(Scale) * Matrix4.CreateRotationX(Rotation.X) * Matrix4.CreateRotationY(Rotation.Y) * Matrix4.CreateRotationZ(Rotation.Z) * Matrix4.CreateTranslation(Position);
        }

        /*
         * For the sake of overrding.
         */
        public override Vector2[] GetTextureCoords()
        {
            return new Vector2[] { };
        }

    }
    public class Cube : Volume 
    {
        private Random randInt;
        private int travel;
        private int x, y, z;
        private Vector3[] col_data;
        public Cube()
        {
            VertCount = 8;
            IndiceCount = 36;
            ColorDataCount = 8;
            randInt = new Random();
            travel = randInt.Next(1,6);
            
            col_data = new Vector3[] {
                new Vector3( 0f, 1f, 0f),
                new Vector3( 1f, 0f, 0f),
                new Vector3( 0f, 0f, 1f),
                new Vector3( 0f, 1f, 0f),
                new Vector3( 1f, 0f, 0f),
                new Vector3( 0f, 0f, 1f)
            };
        }

        /*
         * Something to return the vertex set.
         * One half of the cube is represented in the 6 points,
         * as only 3 points are not shared, the redundant cubes can be discarded
         * 
         */
        public override Vector3[] GetVerts()
        {
            return new Vector3[] {new Vector3(-0.5f, -0.5f,  -0.5f),
                new Vector3(0.5f, -0.5f,  -0.5f),
                new Vector3(0.5f, 0.5f,  -0.5f),
                new Vector3(-0.5f, 0.5f,  -0.5f),
                new Vector3(-0.5f, -0.5f,  0.5f),
                new Vector3(0.5f, -0.5f,  0.5f),
                new Vector3(0.5f, 0.5f,  0.5f),
                new Vector3(-0.5f, 0.5f,  0.5f),
            };
        }
        /*
         * Get the vertexes of the triangles.
         * Each face has 2 triangles, hence only on point-index differs
         * in the pair of triads of vertexes
         * 
         * ex: left consists of 0,2,1 and 0,3,2.
         * Since 2 vertexes are shared onl on differs.
         */
       
        public override int[] GetIndices(int offset = 0)
        {
            int[] inds = new int[] {
                //left
                0, 2, 1,
                0, 3, 2,
                //back
                1, 2, 6,
                6, 5, 1,
                //right
                4, 5, 6,
                6, 7, 4,
                //top
                2, 3, 6,
                6, 3, 7,
                //front
                0, 7, 3,
                0, 4, 7,
                //bottom
                0, 1, 5,
                0, 5, 4
            };

            if (offset != 0)
            {
                for (int i = 0; i < inds.Length; i++)
                {
                    inds[i] += offset;
                }
            }

            return inds;
        }

        /*
         * Something to handle color data.
         */
        public override Vector3[] GetColorData()
        {
            return col_data;
        }

        /*
         * Calculate model matrix.
         */
        public override void CalculateModelMatrix()
        {
            ModelMatrix = Matrix4.CreateScale(Scale) * Matrix4.CreateRotationX(Rotation.X) * Matrix4.CreateRotationY(Rotation.Y) * Matrix4.CreateRotationZ(Rotation.Z) * Matrix4.CreateTranslation(Position);
            if (travel == 1)
                Position += new Vector3(randInt.Next(0,10)/20.0f,0f,0f);
            else if(travel == 2)
                Position += new Vector3(0f, randInt.Next(0, 10) / 20.0f, 0f);
            else if(travel == 3)
                Position += new Vector3(0f, 0f, randInt.Next(0, 10) / 20.0f);
            else if (travel == 4)
                Position += new Vector3(0f, randInt.Next(0, 10) / 20.0f, randInt.Next(0, 10) / 20.0f);
            else if (travel == 5)
                Position += new Vector3(randInt.Next(0, 10) / 20.0f, 0f, randInt.Next(0, 10) / 20.0f);
            else if (travel == 6)
                Position += new Vector3(randInt.Next(0, 10) / 50.0f, randInt.Next(0, 10) / 50.0f,0f);
            else
                Position += new Vector3(randInt.Next(0, 10) / 50.0f, randInt.Next(0, 10) / 50.0f, randInt.Next(0, 10) / 50.0f);
        }

        /*
         * Override from Volume class for GetTextureCoords();
         * Actuall implementation in TexturedCude class.
         */
        public override Vector2[] GetTextureCoords()
        {
            return new Vector2[] { };
        }
    }
    class Sierpinski : Volume
    {
        private Random randInt;
        private int travel;

        public Sierpinski(int numSubdivisions = 1)
        {
            int NumTris = (int)Math.Pow(4, numSubdivisions + 1);

            VertCount = NumTris;
            ColorDataCount = NumTris;
            IndiceCount = 3 * NumTris;

            Tetra twhole = new Tetra(new Vector3(0.0f, 0.0f, 1.0f),  // Apex center 
                            new Vector3(0.943f, 0.0f, -0.333f),  // Base center top
                            new Vector3(-0.471f, 0.816f, -0.333f),  // Base left bottom
                            new Vector3(-0.471f, -0.816f, -0.333f));

            List<Tetra> allTets = twhole.Divide(numSubdivisions);

            int offset = 0;
            foreach (Tetra t in allTets)
            {
                verts.AddRange(t.GetVerts());
                indices.AddRange(t.GetIndices(offset * 4));
                colors.AddRange(t.GetColorData());
                offset++;
            }
            randInt = new Random();
            travel = randInt.Next(1, 6);
        }

        private List<Vector3> verts = new List<Vector3>();
        private List<int> indices = new List<int>();
        private List<Vector3> colors = new List<Vector3>();

        public override Vector3[] GetVerts()
        {
            return verts.ToArray();
        }

        public override Vector3[] GetColorData()
        {
            return colors.ToArray();
        }

        public override int[] GetIndices(int offset = 0)
        {
            int[] inds = indices.ToArray();

            if (offset != 0)
            {
                for (int i = 0; i < inds.Length; i++)
                {
                    inds[i] += offset;
                }
            }

            return inds;
        }

        public override void CalculateModelMatrix()
        {
            ModelMatrix = Matrix4.Scale(Scale) * Matrix4.CreateRotationX(Rotation.X) * Matrix4.CreateRotationY(Rotation.Y) * Matrix4.CreateRotationZ(Rotation.Z) * Matrix4.CreateTranslation(Position);

            if (travel == 1)
                Position += new Vector3(randInt.Next(0, 10) / 20.0f, 0f, 0f);
            else if (travel == 2)
                Position += new Vector3(0f, randInt.Next(0, 10) / 20.0f, 0f);
            else if (travel == 3)
                Position += new Vector3(0f, 0f, randInt.Next(0, 10) / 20.0f);
            else if (travel == 4)
                Position += new Vector3(0f, randInt.Next(0, 10) / 20.0f, randInt.Next(0, 10) / 20.0f);
            else if (travel == 5)
                Position += new Vector3(randInt.Next(0, 10) / 10.0f, 0f, randInt.Next(0, 10) / 10.0f);
            else if (travel == 6)
                Position += new Vector3(randInt.Next(0, 10) / 50.0f, randInt.Next(0, 10) / 50.0f, 0f);     
            else
                Position += new Vector3(1f,0f,0f);  
        }

        /*
         * For the sake of overrding.
         */
        public override Vector2[] GetTextureCoords()
        {
            return new Vector2[] { };
        }
    }
    class TexturedCube : Cube
    {
        public TexturedCube() : base()
        {
            VertCount = 24;
            IndiceCount = 36;
            TextureCoordsCount = 24;
        }

        /*
         * Coordinates of cube vertices, each side having 2 triangles.
         * Therefore 6X2 = 12 triangles.
         */
        public override Vector3[] GetVerts()
        {
            return new Vector3[] {
                //left
                new Vector3(-0.5f, -0.5f,  -0.5f),
                new Vector3(0.5f, 0.5f,  -0.5f),
                new Vector3(0.5f, -0.5f,  -0.5f),
                new Vector3(-0.5f, 0.5f,  -0.5f),
 
                //back
                new Vector3(0.5f, -0.5f,  -0.5f),
                new Vector3(0.5f, 0.5f,  -0.5f),
                new Vector3(0.5f, 0.5f,  0.5f),
                new Vector3(0.5f, -0.5f,  0.5f),
 
                //right
                new Vector3(-0.5f, -0.5f,  0.5f),
                new Vector3(0.5f, -0.5f,  0.5f),
                new Vector3(0.5f, 0.5f,  0.5f),
                new Vector3(-0.5f, 0.5f,  0.5f),
 
                //top
                new Vector3(0.5f, 0.5f,  -0.5f),
                new Vector3(-0.5f, 0.5f,  -0.5f),
                new Vector3(0.5f, 0.5f,  0.5f),
                new Vector3(-0.5f, 0.5f,  0.5f),
 
                //front
                new Vector3(-0.5f, -0.5f,  -0.5f), 
                new Vector3(-0.5f, 0.5f,  0.5f), 
                new Vector3(-0.5f, 0.5f,  -0.5f),
                new Vector3(-0.5f, -0.5f,  0.5f),
 
                //bottom
                new Vector3(-0.5f, -0.5f,  -0.5f), 
                new Vector3(0.5f, -0.5f,  -0.5f),
                new Vector3(0.5f, -0.5f,  0.5f),
                new Vector3(-0.5f, -0.5f,  0.5f) 
            };
        }

        public override int[] GetIndices(int offset = 0)
        {
            int[] inds = new int[] {
                //left
                0,1,2,0,3,1, 
                //back
                4,5,6,4,6,7, 
                //right
                8,9,10,8,10,11, 
                //top
                13,14,12,13,15,14,
                //front
                16,17,18,16,19,17,
                //bottom 
                20,21,22,20,22,23
            };

            if (offset != 0)
            {
                for (int i = 0; i < inds.Length; i++)
                {
                    inds[i] += offset;
                }
            }

            return inds;
        }

        public override Vector2[] GetTextureCoords()
        {
            return new Vector2[] {
                // left
                new Vector2(0.0f, 0.0f),
                new Vector2(-1.0f, 1.0f),
                new Vector2(-1.0f, 0.0f),
                new Vector2(0.0f, 1.0f),
 
                // back
                new Vector2(0.0f, 0.0f),
                new Vector2(0.0f, 1.0f),
                new Vector2(-1.0f, 1.0f),
                new Vector2(-1.0f, 0.0f),
 
                // right
                new Vector2(-1.0f, 0.0f),
                new Vector2(0.0f, 0.0f),
                new Vector2(0.0f, 1.0f),
                new Vector2(-1.0f, 1.0f),
 
                // top
                new Vector2(0.0f, 0.0f),
                new Vector2(0.0f, 1.0f),
                new Vector2(-1.0f, 0.0f),
                new Vector2(-1.0f, 1.0f),
 
                // front
                new Vector2(0.0f, 0.0f),
                new Vector2(1.0f, 1.0f),
                new Vector2(0.0f, 1.0f),
                new Vector2(1.0f, 0.0f),
 
                // bottom
                new Vector2(0.0f, 0.0f),
                new Vector2(0.0f, 1.0f),
                new Vector2(-1.0f, 1.0f),
                new Vector2(-1.0f, 0.0f)
            };
        }
    }
    class Game : GameWindow
    {
        private float timer = 1.0f;
        //------------------------------------------------------------------------------------------------------------------

        /*
         * shaders hold the list of all shaders. ShaderUnit objects contain all the shader attributes such as:
         *  pgmID,vsID,fsID,vcol,vpos etc....
         */
        Dictionary<string, ShaderUnit> shaders = new Dictionary<string, ShaderUnit>();
        string activeShader;

        /*
         * Stores vertexes,corresponding colors and the model
         * to be fed into the buffers.
         */
        private Vector3[] vertdata;
        private Vector3[] coldata;

        /*
         * Stores the seperate volume objects to be rendered.
         */
        List<Volume> objects = new List<Volume>();
        HashSet<float[]> obPos = new HashSet<float[]>();

        /*
         * 3D
         */
        private int[] indicedata;
        private int ibo_elements;

        /*
         * Camera variables
         */
        private Camera cam = new Camera();
        Vector2 lastMousePos = new Vector2();

        /*
         * textures Dictionary  keeps track of textures as (textureID,ID string)
         * texcoorddata stores information about texture coordinates.
         */
        Dictionary<string, int> textures = new Dictionary<string, int>();
        Vector2[] texcoorddata;
        //------------------------------------------------------------------------------------------------------------------
        /*
         * Creates new 'Program' object return pgmID, load shaders and get references.
         */

        public int objCount;
        public bool renderMore;
        public bool renderOnCommand;
        Random rand;

        void initProgram()
        {

            lastMousePos = new Vector2(Mouse.X, Mouse.Y);
            GL.GenBuffers(1, out ibo_elements);

            
            /*
             * Initial Shader loading: load shaders from file. Deleted the loadShader() method.
             */


            GL.Enable(EnableCap.Texture2D);
            shaders.Add("default", new ShaderUnit("vs.glsl", "fs.glsl", true));
            shaders.Add("textured", new ShaderUnit("vs_tex.glsl", "fs_tex.glsl", true));
            activeShader = "default";

            textures.Add("opentksquare.png", LoadImageFile("opentksquare.png"));
            textures.Add("opentksquare2.png", LoadImageFile("opentksquare2.png"));

            GL.ShadeModel(ShadingModel.Smooth);
            
            cam.Position += new Vector3(0f, 0f, 3f);
        }
        /*------------------------------------------------------------------------------------------------------------------*/

        /**
         * Start new Game @30 screen fps, swapping buffers @ 30/sec
        */
       
        static void Main(string[] args)
        {
            using (Game game = new Game())
            {
                // This runs asynchronously on a thread.
                WMPLib.WindowsMediaPlayer wplayer = new WMPLib.WindowsMediaPlayer();
                wplayer.URL = "Sounds/stay.mp3";
                wplayer.controls.play();

                game.Run(50, 30);                
            }
        }

        public Game() : base(1300, 900, new GraphicsMode(0, 24, 0, 32))
        {
            Console.WriteLine("32x AA");
        }

        /*
         * Function to load textures
         */
        int LoadImage(Bitmap texture)
        {
            int TextureID = GL.GenTexture();
            /*
             * Binding the bitmap to textureID so we can use it more easily.
             */
            GL.BindTexture(TextureTarget.Texture2D, TextureID);
            /*
             * C# is something else >_>
             */
            BitmapData data = texture.LockBits(new System.Drawing.Rectangle(0, 0, texture.Width, texture.Height),
            ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            texture.UnlockBits(data);

            /*
             * First we make openGL create a textureID we can use to work with. Then we bind it as a 2D image and send it to the GPU to get the pixels.
             * Next we call GL.GenerateMipmap() to create MipMaps of the original bitmap. Mipmaps are bitmaps of variying sizes which the GPU creates inorder to
             * lighten the load on th GPU.
             */
            GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
            Console.WriteLine("Read textures");
            Console.WriteLine("Texture id : " + TextureID);
            return TextureID;
        }

        /*
         * Function overload >_>
         */
        int LoadImageFile(string filename)
        {
            try
            {
                Bitmap file = new Bitmap(filename);
                return LoadImage(file);
            }
            catch (FileNotFoundException e)
            {
                return -1;
            }
        }


        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            //WindowBorder = WindowBorder.Hidden;
            //WindowState = WindowState.Fullscreen;

            System.Windows.Forms.Cursor.Hide();

            objCount = 0;
            renderMore = true;
            renderOnCommand = false;
            rand = new Random();

            initProgram();

            Title = "Cubes in 3D by OpenTK";
            GL.ClearColor(Color.Snow);
            GL.PointSize(1f);
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);
            GL.Viewport(0, 0, Width, Height);
            GL.Enable(EnableCap.DepthTest);
            GL.Enable(EnableCap.Texture2D);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit | ClearBufferMask.AccumBufferBit | ClearBufferMask.StencilBufferBit);

         
            /*
             *  Activating and enabling attribute arrays for the active ShaderUnit.
             */
            shaders[activeShader].EnableVertexAttribArrays();

            int indiceat = 0;

            /*
             * Render TextureCubes
             */
            foreach (Volume v in objects)
            {
                GL.Enable(EnableCap.Texture2D);
                GL.BindTexture(TextureTarget.Texture2D, v.TextureID);
                GL.UniformMatrix4(shaders[activeShader].GetUniform("modelview"), false, ref v.ModelViewProjectionMatrix);

                if (shaders[activeShader].GetAttribute("maintexture") != -1)
                {
                    GL.Uniform1(shaders[activeShader].GetAttribute("maintexture"), v.TextureID);
                }
                GL.Normal3(new Vector3(0, 0, 1));

                GL.DrawElements(BeginMode.Triangles, v.IndiceCount, DrawElementsType.UnsignedInt, indiceat * sizeof(uint));
                GL.Disable(EnableCap.Texture2D);
                indiceat += v.IndiceCount;
            }

            /*
             * Disabling attribute arrays for the shader.
             */
            shaders[activeShader].DisableVertexAttribArrays();

            GL.Flush();
            SwapBuffers();
        }

        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            // e.Time holds time sincec last update.
            timer += (float)e.Time;

            base.OnUpdateFrame(e);

            List<Vector3> verts = new List<Vector3>();
            List<int> inds = new List<int>();
            List<Vector3> colors = new List<Vector3>();
            List<Vector2> texcoords = new List<Vector2>();
            /*
             * Camera focus check.
             */
            if (Focused)
            {
                Vector2 delta = lastMousePos - new Vector2(OpenTK.Input.Mouse.GetState().X, OpenTK.Input.Mouse.GetState().Y);

                cam.AddRotation(delta.X, delta.Y);
                ResetCursor();
            }

            int vertcount = 0;

            foreach (Volume v in objects)
            {
                verts.AddRange(v.GetVerts().ToList());
                inds.AddRange(v.GetIndices(vertcount).ToList());
                colors.AddRange(v.GetColorData().ToList());
                texcoords.AddRange(v.GetTextureCoords());
                vertcount += v.VertCount;
            }

            vertdata = verts.ToArray();
            indicedata = inds.ToArray();
            coldata = colors.ToArray();
            texcoorddata = texcoords.ToArray();
            /*
             * Binding data to ShaderUnit object.
             */
            GL.BindBuffer(BufferTarget.ArrayBuffer, shaders[activeShader].GetBuffer("vPosition"));

            GL.BufferData<Vector3>(BufferTarget.ArrayBuffer, (IntPtr)(vertdata.Length * Vector3.SizeInBytes), vertdata, BufferUsageHint.StaticDraw);
            GL.VertexAttribPointer(shaders[activeShader].GetAttribute("vPosition"), 3, VertexAttribPointerType.Float, false, 0, 0);

            /*
             * Sending color data
             */
            if (shaders[activeShader].GetAttribute("vColor") != -1)
            {
                GL.BindBuffer(BufferTarget.ArrayBuffer, shaders[activeShader].GetBuffer("vColor"));
                GL.BufferData<Vector3>(BufferTarget.ArrayBuffer, (IntPtr)(coldata.Length * Vector3.SizeInBytes), coldata, BufferUsageHint.StaticDraw);
                GL.VertexAttribPointer(shaders[activeShader].GetAttribute("vColor"), 3, VertexAttribPointerType.Float, true, 0, 0);
            }

            /*
             * Sending texture data
             */
            if (shaders[activeShader].GetAttribute("texcoord") != -1)
            {
                GL.BindBuffer(BufferTarget.ArrayBuffer, shaders[activeShader].GetBuffer("texcoord"));
                GL.BufferData<Vector2>(BufferTarget.ArrayBuffer, (IntPtr)(texcoorddata.Length * Vector2.SizeInBytes), texcoorddata, BufferUsageHint.StaticDraw);
                GL.VertexAttribPointer(shaders[activeShader].GetAttribute("texcoord"), 2, VertexAttribPointerType.Float, true, 0, 0);
            }
            /*
             * This code is the same for ShaderUnit object also.
             */
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, ibo_elements);
            GL.BufferData(BufferTarget.ElementArrayBuffer, (IntPtr)(indicedata.Length * sizeof(int)), indicedata, BufferUsageHint.StaticDraw    );

            /*
             * Rotate model view
             */
            float i = 0.05f;
            foreach (Volume v in objects)
            {
                v.Rotation += new Vector3(i, i + 0.05f * (float)Math.Sin(timer * 0.25), i);
                i += 0.00000005f;
            }

            while(objCount > 0 && renderMore)
            {                            
                TexturedCube c = new TexturedCube();
                c.TextureID = textures["opentksquare.png"];
                float x = (float)rand.Next(-800, 800);
                float y = (float)rand.Next(-800, 800);
                float z = (float)rand.Next(-800, 800);

                c.Position = new Vector3(x, y, z);
                c.Rotation = new Vector3((float)rand.Next(0, 6), (float)rand.Next(0, 6), (float)rand.Next(0, 6));
                c.Scale = Vector3.One * ((float)rand.NextDouble() + 1.3f);
                objects.Add(c);

                Sierpinski s = new Sierpinski();
                s.Position = new Vector3((float)rand.Next(-400, 450) + objects.Count() / 50, (float)rand.Next(-450, 450) + objects.Count() / 100, (float)rand.Next(-150, 1150) + objects.Count() / 100);
                s.Rotation = new Vector3((float)rand.Next(0, 6), (float)rand.Next(0, 6), (float)rand.Next(0, 6));
                s.Scale = Vector3.One * ((float)rand.NextDouble() + 0.4f);
                objects.Add(s);

                objCount -= 2;
            }
            if(renderOnCommand)
            {
                renderOnCommand = false;
                for (int j = 0; j < 200; j++)
                {
                    Cube c = new Cube();
                    float x = (float)rand.Next(-800, 800);
                    float y = (float)rand.Next(-800, 800);
                    float z = (float)rand.Next(-800, 800);

                    c.Position = new Vector3(x, y, z);
                    c.Rotation = new Vector3((float)rand.Next(0, 6), (float)rand.Next(0, 6), (float)rand.Next(0, 6));
                    c.Scale = Vector3.One * ((float)rand.NextDouble() + 1.3f);
                    objects.Add(c);

                    Sierpinski s = new Sierpinski();
                    s.Position = new Vector3((float)rand.Next(-400, 450) + objects.Count() / 50, (float)rand.Next(-450, 450) + objects.Count() / 100, (float)rand.Next(-150, 1150) + objects.Count() / 100);
                    s.Rotation = new Vector3((float)rand.Next(0, 6), (float)rand.Next(0, 6), (float)rand.Next(0, 6));
                    s.Scale = Vector3.One * ((float)rand.NextDouble() + 0.4f);
                    objects.Add(s);
                }
            }
            foreach (Volume v in objects)
            {
                v.CalculateModelMatrix();
                /*
                 * v.ViewProjectionMatrix = Matrix4.CreatePerspectiveFieldOfView(1.3f, ClientSize.Width / (float)ClientSize.Height, 1.0f, 40.0f);
                 * is assigmed to the camera's matrix.
                 */
                v.ViewProjectionMatrix = cam.GetViewMatrix() * Matrix4.CreatePerspectiveFieldOfView(1.13f, ClientSize.Width / (float)ClientSize.Height, 0.001f, 2000.0f);
                v.ModelViewProjectionMatrix = v.ModelMatrix * v.ViewProjectionMatrix;
            }

            /*
             * Activate ShaderUnit object
             */
            GL.UseProgram(shaders[activeShader].ProgramID);
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
        }

        /*
         * Camera controls.
         */
        protected override void OnKeyPress(KeyPressEventArgs e)
        {
            base.OnKeyPress(e);
            if(objects.Count < 2000)
                objCount++;
            else
            {
                GL.ClearColor(Color.Black);
                renderMore = false;
            }                           
                       
            if (e.KeyChar == 27)
            {
                Exit();
            }

            switch (e.KeyChar)
            {
                case 'w':
                    cam.Move(0f, 0.3f, 0f);
                    break;
                case 'a':
                    cam.Move(-0.3f, 0f, 0f);
                    break;
                case 's':
                    cam.Move(0f, -0.3f, 0f);
                    break;
                case 'd':
                    cam.Move(0.3f, 0f, 0f);
                    break;
                case 'q':
                    cam.Move(0f, 0f, 0.3f);
                    break;
                case 'e':
                    cam.Move(0f, 0f, -0.3f);
                    break;
                case 't': {        
                    Task.Factory.StartNew(() => zoomT());
                    break;
                }

                case 'g': {
                    Task.Factory.StartNew(() => zoomF());
                    break;
                }

                case 'f': {
                    Task.Factory.StartNew(() => zoomG());
                    break;
                }

                case 'h': {
                    Task.Factory.StartNew(() => zoomH());
                    break;
                }

                case 'r': {
                    Task.Factory.StartNew(() => zoomR());
                    break;
                }

                case 'y': {
                    Task.Factory.StartNew(() => zoomY());
                    break;
                }

                case 'z':
                    renderOnCommand = true;
                    break;

                case '0':
                    GL.ClearColor(Color.Snow);
                    break;
                case '1':
                    GL.ClearColor(Color.Transparent);
                    break;
                case '2':
                    GL.ClearColor(Color.WhiteSmoke);
                    break;
                case '3':
                    GL.ClearColor(Color.MidnightBlue);
                    break;
                case '4':
                    GL.ClearColor(Color.NavajoWhite);
                    break;
                case '5':
                    GL.ClearColor(Color.AntiqueWhite);
                    break;
                case '6':
                    GL.ClearColor(Color.OrangeRed);
                    break;
                case '7':
                    GL.ClearColor(Color.PaleGoldenrod);
                    break;
                case '8':
                    GL.ClearColor(Color.LavenderBlush);
                    break;
                case '9':
                    GL.ClearColor(Color.Black);
                    break;
                default:
                    Exit();
                    break;
            }
        }
        void zoomT()
        {
            for(int i=0;i<1000;i++)
            {
                cam.Move(0f, 0.05f, 0f);
                Thread.Sleep(1);
            }
        }
        void zoomF()
        {
            for (int i = 0; i < 1000; i++)
            {
                cam.Move(-0.0000000005f, 0f, 0f);
                Thread.Sleep(1);
            }
        }
        void zoomG()
        {
            for (int i = 0; i < 1000; i++)
            {
                cam.Move(0f, -0.0000000005f, 0f);
                Thread.Sleep(1);
            }
        }
        void zoomH()
        {
            for (int i = 0; i < 1000; i++)
            {
                cam.Move(0.0000000005f, 0f, 0f);
                Thread.Sleep(1);
            }
        }

        void zoomR()
        {
            for (int i = 0; i < 1000; i++)
            {
                cam.Move(0f, 0f, 0.0000000005f);
                Thread.Sleep(1);
            }
        }
        void zoomY()
        {
            for (int i = 0; i < 1000; i++)
            {
                cam.Move(0f, 0f, -0.0000000005f);
                Thread.Sleep(1);
            }
        }
        void ResetCursor()
        {
            OpenTK.Input.Mouse.SetPosition(Bounds.Left + Bounds.Width / 2, Bounds.Top + Bounds.Height / 2);
            lastMousePos = new Vector2(OpenTK.Input.Mouse.GetState().X, OpenTK.Input.Mouse.GetState().Y);
        }

        public static void DrawXYZAxis(double length)
        {

                GL.Begin(BeginMode.Lines);

                // X
                GL.Color3(1.0, 0, 0);
                GL.Vertex3(0, 0, 0);
                GL.Vertex3(length, 0, 0);

                // Y
                GL.Color3(0, 1.0, 0);
                GL.Vertex3(0, 0, 0);
                GL.Vertex3(0, 100 * length, 0);

                // Z
                GL.Color3(0, 0, 1.0);
                GL.Vertex3(0, 0, 0);
                GL.Vertex3(0, 0, length);

                GL.End();

        }
        /*
         * Reset cursor on Focus change.
         */
        protected override void OnFocusedChanged(EventArgs e)
        {
            base.OnFocusedChanged(e);

            if (Focused)
            {
                ResetCursor();
            }
        }
    }
}