using System;
using LearnOpenTK.Common;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenTK.Windowing.Desktop;
using System.Diagnostics;

using System.IO;
using System.Globalization;

namespace OpenTKLab

{
    public class Window : GameWindow
    {
        private int CountTrngl;
        private int CountBubles = 100;
        private int Square = 2;

        private float[] _vertices;

        private Vector3[] _BublesPositions;
        private float[] _BublesPhase;

        private float BublesSpeed = 1.5f;
        private float UseBublesSpeed;
        private float Oscillation = 4f;

        private readonly Vector3 _lightPos = new Vector3(-3f, -3f, -1f);

        private int _vertexBufferObject;

        private int _vaoModel;

        private Shader shader;

        private Camera _camera;

        private bool _firstMove = true;

        private Vector2 _lastPos;

        private float tmpphase = 0;

        public Window(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings)
            : base(gameWindowSettings, nativeWindowSettings)
        {
        }

        protected override void OnLoad()
        {
            base.OnLoad();
            Read();

            GL.Enable(EnableCap.DepthTest);

            _vertexBufferObject = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBufferObject);
            GL.BufferData(BufferTarget.ArrayBuffer, _vertices.Length * sizeof(float), _vertices, BufferUsageHint.StaticDraw);

            shader = new Shader("Shaders/shader.vert", "Shaders/shader.frag");

            {
                _vaoModel = GL.GenVertexArray();
                GL.BindVertexArray(_vaoModel);

                var positionLocation = shader.GetAttribLocation("aPos");
                GL.EnableVertexAttribArray(positionLocation);
                GL.VertexAttribPointer(positionLocation, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), 0);

                var normalLocation = shader.GetAttribLocation("aNormal");
                GL.EnableVertexAttribArray(normalLocation);
                GL.VertexAttribPointer(normalLocation, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), 3 * sizeof(float));

            }
            
            GL.ClearColor(30/255f, 30/255f, 112f/255f, 1.0f);

            shader.SetVector3("ColorObj", new Vector3(0 / 255f, 170f / 255f, 255 / 255f));
          
            _camera = new Camera(new Vector3(0f,5f,0f), Size.X / (float)Size.Y);

            CursorGrabbed = true;
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);

            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            GL.BindVertexArray(_vaoModel);
            
            shader.Use();

            shader.SetMatrix4("view", _camera.GetViewMatrix());
            shader.SetMatrix4("projection", _camera.GetProjectionMatrix());

            shader.SetVector3("viewPos", _camera.Position);

            shader.SetVector3("material.ambient", new Vector3(0.2f));
            shader.SetVector3("material.diffuse", new Vector3(0.6f));
            shader.SetVector3("material.specular", new Vector3(1.0f));
            shader.SetFloat("material.shininess", 16.0f);

            shader.SetVector3("light.direction", new Vector3(_lightPos));
            shader.SetVector3("light.ambient", new Vector3(1.0f));
            shader.SetVector3("light.diffuse", new Vector3(1.0f));
            shader.SetVector3("light.specular", new Vector3(1.0f));
      
            Random rnd = new Random();
            Vector3 tmp;
            for (int i = 0; i < CountBubles; i++)
            {
                //_BublesPositions[i].Y += UseBublesSpeed * (float)e.Time;
                //if (_BublesPositions[i].Y > 10f + (float)rnd.NextDouble() * 2)
                //    _BublesPositions[i].Y = -10f - (float)rnd.NextDouble() * 2;
                _BublesPhase[i]+= UseBublesSpeed * (float)e.Time;
                tmp = _BublesPositions[i];
                tmp.X = (float)Math.Sin(_BublesPhase[i]) * Square;
                tmp.Z = (float)Math.Cos(_BublesPhase[i]) * Square;

                //tmp.X += (float)Math.Sin(tmpphase) * Oscillation;
                //tmp.Z += (float)Math.Cos(tmpphase) * Oscillation;

                Matrix4 model = Matrix4.CreateTranslation(new Vector3(1.0f));
                //model[0, 0] += (tmp.Y+12f) / 10; 
                //model[1, 1] += (tmp.Y+12f) / 10; 
                //model[2, 2] += (tmp.Y+12f) / 10; 

                model*=Matrix4.CreateTranslation(tmp);

                shader.SetMatrix4("model", model);

                GL.DrawArrays(PrimitiveType.Triangles, 0, CountTrngl);
            
            }

            SwapBuffers();
        }

        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            base.OnUpdateFrame(e);

            if (!IsFocused)
            {
                _firstMove = true;
                return;
            }

            var input = KeyboardState;

            if (input.IsKeyDown(Keys.Escape))
            {
                Close();
            }
            if (input.IsKeyPressed(Keys.P))
            {
                UseBublesSpeed = BublesSpeed - UseBublesSpeed;

            }

            const float cameraSpeed = 2.5f;
            const float sensitivity = 0.2f;

            if (input.IsKeyDown(Keys.W))
            {
                _camera.Position += _camera.Front * cameraSpeed * (float)e.Time; // Forward
            }
            if (input.IsKeyDown(Keys.S))
            {
                _camera.Position -= _camera.Front * cameraSpeed * (float)e.Time; // Backwards
            }
            if (input.IsKeyDown(Keys.D))
            {
                _camera.Position += _camera.Right * cameraSpeed * (float)e.Time; // Right
            }
            if (input.IsKeyDown(Keys.A))
            {
                _camera.Position -= _camera.Right * cameraSpeed * (float)e.Time; // Left
            }
            if (input.IsKeyDown(Keys.Space))
            {
                _camera.Position += _camera.Up * cameraSpeed * (float)e.Time; // Up
            }
            if (input.IsKeyDown(Keys.LeftShift))
            {
                _camera.Position -= _camera.Up * cameraSpeed * (float)e.Time; // Down
            }

            var mouse = MouseState;

            if (_firstMove)
            {
                _lastPos = new Vector2(mouse.X, mouse.Y);
                _firstMove = false;
            }
            else
            {
                var deltaX = mouse.X - _lastPos.X;
                var deltaY = mouse.Y - _lastPos.Y;
                _lastPos = new Vector2(mouse.X, mouse.Y);

                _camera.Yaw += deltaX * sensitivity;
                _camera.Pitch -= deltaY * sensitivity;
            }
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            base.OnMouseWheel(e);

            _camera.Fov -= e.OffsetY;
        }

        protected override void OnResize(ResizeEventArgs e)
        {
            base.OnResize(e);

            GL.Viewport(0, 0, Size.X, Size.Y);
            _camera.AspectRatio = Size.X / (float)Size.Y;
        }

        protected void Read() {
            UseBublesSpeed = BublesSpeed;
            StreamReader sr = new StreamReader("C:\\Users\\artem\\Desktop\\OpenTKLab\\OpenTKLab\\buble_16K.txt");
            String line = sr.ReadLine();
            CountTrngl = int.Parse(line, CultureInfo.InvariantCulture);
            _vertices = new float[CountTrngl*6];
            for (int i = 0; i < CountTrngl*6; i++) { 
                line= sr.ReadLine();
                _vertices[i] = float.Parse(line, CultureInfo.InvariantCulture)/7f;
            }
            _BublesPositions = new Vector3[CountBubles]; 
            _BublesPhase = new float[CountBubles]; 
            Random random = new Random();

            for (int i = 0; i < CountBubles; i++) {
                _BublesPhase[i] = (float)random.NextDouble()*8;
                _BublesPositions[i] = 
                    new Vector3((float)Math.Sin(_BublesPhase[i]) * Square,
                                0,
                                (float)Math.Cos(_BublesPhase[i]) * Square);
            }
        }
    }
}
