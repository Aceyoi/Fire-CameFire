using System;
using System.Drawing;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualBasic.Logging;


namespace WinFormsApp13
{
    public partial class Form1 : Form
    {
        private Bitmap fireBitmap;
        private int width = 776;
        private int height = 364;
        private Timer timer;
        private Random random = new Random();
        private List<FireParticle3D> particles = new List<FireParticle3D>();
        private List<Log> logs = new List<Log>();
        private bool fireAnimationEnabled = false;
        private float cameraAngle = 0;
        private float cameraDistance = 500;
        private float cameraAngleY = 0; // Угол вокруг оси Y (горизонтальное вращение)
        private float cameraAngleX = 0; // Угол вокруг оси X (вертикальное вращение)
        private Point3D cameraPosition = new Point3D(0, 150, 0);
        private List<FloorTile> floorTiles = new List<FloorTile>();
        private float gravity = 0.02f;
        private bool isDragging = false;
        private const float MaxCameraAngleX = MathHelper.PiOver2 * 0.1f; // Максимальный угол наклона вниз
        private const float MinCameraAngleX = -MathHelper.PiOver2 * 1f; // Минимальный угол наклона вверх
        private Point lastMousePosition;

        public Form1()
        {
            InitializeComponent();
            this.DoubleBuffered = true;

            // Настройка элементов
            pictureBox1.Size = new Size(width, height);

            // Инициализация битмапа
            fireBitmap = new Bitmap(width, height);

            // Создание пола
            CreateFloorGrid(400, 40);

            // Создаем бревна для костра
            //CreateCampfireLogs();

            // Настройка таймера
            timer = new Timer { Interval = 16 }; // ~60 FPS
            timer.Tick += (s, e) => UpdateFire();
            timer.Start();

            pictureBox1.MouseDown += PictureBox1_MouseDown;
            pictureBox1.MouseMove += PictureBox1_MouseMove;
            pictureBox1.MouseUp += PictureBox1_MouseUp;
            pictureBox1.MouseWheel += PictureBox1_MouseWheel;
        }

        private void PictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                isDragging = true;
                lastMousePosition = e.Location;
            }
        }

        private void PictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDragging)
            {
                int deltaX = e.X - lastMousePosition.X;
                int deltaY = e.Y - lastMousePosition.Y;

                cameraAngleY += deltaX * 0.01f;
                cameraAngleX = Math.Max(MinCameraAngleX,
                                Math.Min(MaxCameraAngleX,
                                cameraAngleX - deltaY * 0.01f));

                lastMousePosition = e.Location;
            }
        }

        private void PictureBox1_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                isDragging = false;
            }
        }

        private void PictureBox1_MouseWheel(object sender, MouseEventArgs e)
        {
            cameraDistance = Math.Max(100, Math.Min(1000, cameraDistance - e.Delta / 2));
        }
        private void CreateCampfireLogs()
        {
            // Первое бревно (горизонтальное)
            logs.Add(new Log
            {
                X = 0,
                Z = 0,
                Size = new Size3D(120, 20, 20),
                Color = Color.FromArgb(255, 101, 67, 33)
            });

            // Второе бревно (перекрещивающееся)
            logs.Add(new Log
            {
                X = 0,
                Z = 0,
                Rotation = 90,
                Size = new Size3D(120, 20, 20),
                Color = Color.FromArgb(255, 92, 64, 51)
            });
        }
        private void CreateFloorGrid(int size, int tileSize)
        {
            for (int x = -size; x <= size; x += tileSize)
            {
                for (int z = -size; z <= size; z += tileSize)
                {
                    floorTiles.Add(new FloorTile
                    {
                        X = x,
                        Z = z,
                        Size = tileSize,
                        BaseColor = Color.FromArgb(70, 70, 70)
                    });
                }
            }
        }

        private void AddParticles(int count)
        {
            for (int i = 0; i < count; i++)
            {
                particles.Add(new FireParticle3D
                {
                    X = (float)(random.NextDouble() - 0.5) * 100,
                    Y = 0,
                    VelocityX = (float)(random.NextDouble() - 0.5) * 0.8f,
                    VelocityY = (float)random.NextDouble() * 2f + 0.5f,
                    //VelocityZ = (float)(random.NextDouble() - 0.5) * 0.8f,
                    Life = random.Next(40, 100),
                    Size = random.Next(4, 10),
                    Intensity = random.Next(200, 256)
                });
                particles.Add(new FireParticle3D
                {
                    Y = 0,
                    Z = (float)(random.NextDouble() - 0.5) * 100,
                    //VelocityX = (float)(random.NextDouble() - 0.5) * 0.8f,
                    VelocityY = (float)random.NextDouble() * 2f + 0.5f,
                    VelocityZ = (float)(random.NextDouble() - 0.5) * 0.8f,
                    Life = random.Next(40, 100),
                    Size = random.Next(4, 10),
                    Intensity = random.Next(200, 256)
                });
            }
        }

        private void UpdateFire()
        {
            // Генерация новых частиц
            if (fireAnimationEnabled)
            {
                AddParticles(15);
            }

            // Обновление физики частиц
            UpdateParticles();

            // Отрисовка сцены
            Draw3DScene();
        }

        private void UpdateParticles()
        {
            for (int i = particles.Count - 1; i >= 0; i--)
            {
                var p = particles[i];

                // Применяем физику
                p.VelocityY -= gravity;
                p.X += p.VelocityX;
                p.Y += p.VelocityY;
                p.Z += p.VelocityZ;

                // Затухание
                p.Intensity = (int)(p.Intensity * 0.97f);
                p.Life--;

                // Удаление старых частиц
                if (p.Life <= 0 || p.Intensity < 10)
                    particles.RemoveAt(i);
            }
        }

        private void Draw3DScene()
        {
            using (var g = Graphics.FromImage(fireBitmap))
            {
                g.Clear(Color.Black);

                // Сортируем объекты по глубине
                var floorToDraw = floorTiles.OrderBy(t => GetDepth(t.X, 0, t.Z));
                var logToDraw = logs.OrderBy(l => GetDepth(l.X, l.Y, l.Z));
                var particlesToDraw = particles.OrderBy(p => GetDepth(p.X, p.Y, p.Z));

                // Рисуем пол
                foreach (var tile in floorToDraw)
                {
                    DrawFloorTile(g, tile);
                }

                // Рисуем бревна
                foreach (var log in logToDraw)
                {
                    DrawLog(g, log);
                }

                // Рисуем частицы
                foreach (var p in particlesToDraw)
                {
                    DrawParticle(g, p);
                }
            }
            pictureBox1.Image = fireBitmap;
        }

        private void DrawLog(Graphics g, Log log)
        {

            var (screenX, screenY, screenSize) = Project3DTo2D(log.X, 0, log.Z, Math.Max(log.Size.Width, log.Size.Height));

            // Сохраняем исходное преобразование
            var oldTransform = g.Transform;

            // Применяем поворот
            g.TranslateTransform(screenX, screenY);
            g.RotateTransform(log.Rotation);

            // Рисуем бревно
            using (var brush = new SolidBrush(log.Color))
            {
                g.FillRectangle(brush,
                    -log.Size.Width / 2,
                    -log.Size.Height / 2,
                    log.Size.Width,
                    log.Size.Height);
            }

            // Текстура бревна
            using (var pen = new Pen(Color.FromArgb(100, 0, 0, 0), 2))
            {
                for (int i = 0; i < 5; i++)
                {
                    float yPos = -log.Size.Height / 2 + i * (log.Size.Height / 4);
                    g.DrawLine(pen,
                        -log.Size.Width / 2,
                        yPos,
                        log.Size.Width / 2,
                        yPos);
                }
            }

            // Восстанавливаем преобразование
            g.Transform = oldTransform;
        }
        private void DrawFloorTile(Graphics g, FloorTile tile)
        {
            var (screenX, screenY, screenSize) = Project3DTo2D(tile.X, 0, tile.Z, tile.Size);

            // Затемнение по расстоянию
            float depth = GetDepth(tile.X, 0, tile.Z);
            float darkenFactor = 1.0f - Math.Min(1, depth / 1000f);
            Color tileColor = Color.FromArgb(
                (int)(tile.BaseColor.R * darkenFactor),
                (int)(tile.BaseColor.G * darkenFactor),
                (int)(tile.BaseColor.B * darkenFactor));

            using (var brush = new SolidBrush(Color.Peru))
            {
                g.FillRectangle(brush, screenX - screenSize / 2, screenY - screenSize / 2, 50, 100);
            }
        }

        private void DrawParticle(Graphics g, FireParticle3D p)
        {
            var (screenX, screenY, screenSize) = Project3DTo2D(p.X, p.Y, p.Z, p.Size);

            // Сохраняем исходное преобразование
            var oldTransform = g.Transform;

            // Применяем поворот
            g.TranslateTransform(screenX, screenY);

            // Цвет
            int alpha = (int)(255 * (p.Life / 100f));
            var color = Color.FromArgb(alpha, 255, (int)(255 * (p.Intensity / 255f)), 0);

            // Рисуем повернутый квадрат
            using (var brush = new SolidBrush(color))
            {
                g.FillRectangle(brush, -screenSize / 2, -screenSize / 2, screenSize, screenSize);
            }

            // Восстанавливаем преобразование
            g.Transform = oldTransform;
        }

        private (float screenX, float screenY, float screenSize) Project3DTo2D(float x, float y, float z, float size)
        {
            // Вращение вокруг оси Y (горизонтальное)
            float rotatedX = (float)(x * Math.Cos(cameraAngleY) - z * Math.Sin(cameraAngleY));
            float rotatedZ = (float)(x * Math.Sin(cameraAngleY) + z * Math.Cos(cameraAngleY));

            // Вращение вокруг оси X (вертикальное)
            float finalY = (float)(y * Math.Cos(cameraAngleX) - rotatedZ * Math.Sin(cameraAngleX));
            float finalZ = (float)(y * Math.Sin(cameraAngleX) + rotatedZ * Math.Cos(cameraAngleX));

            // Перспективная проекция
            float scale = cameraDistance / (cameraDistance + finalZ);
            float screenX = width / 2 + (rotatedX - cameraPosition.X) * scale;
            float screenY = height / 2 - (finalY - cameraPosition.Y) * scale;
            float screenSize = size * scale;

            return (screenX, screenY, screenSize);
        }
        private float GetDepth(float x, float y, float z)
        {
            return (float)(x * Math.Sin(cameraAngle) + z * Math.Cos(cameraAngle));
        }

        // Обработчики кнопок
        private void button1_Click_1(object sender, EventArgs e) => AddParticles(1);
        private void button2_Click_1(object sender, EventArgs e) => fireAnimationEnabled = !fireAnimationEnabled;
        private void button3_Click(object sender, EventArgs e)
        {
            // Очищаем частицы
            particles.Clear();

            // Останавливаем анимацию огня
            fireAnimationEnabled = false;
            button2.Text = "Включить огонь";
            timer.Stop();
            pictureBox1.Image = fireBitmap;
            timer.Start();
        }
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            timer?.Stop();
            fireBitmap?.Dispose();
        }
    }

    static class MathHelper
    {
        public const float PiOver2 = (float)(Math.PI / 2);
    }
    class FloorTile
    {
        public int X { get; set; }
        public int Z { get; set; }
        public int Size { get; set; }
        public Color BaseColor { get; set; }
    }

    class FireParticle3D
    {
        public float X, Y, Z;
        public float VelocityX, VelocityY, VelocityZ;
        public int Life;
        public int Size;
        public int Intensity;
    }

    class Point3D
    {
        public float X, Y, Z;
        public Point3D(float x, float y, float z) => (X, Y, Z) = (x, y, z);
    }

    class Log
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public Size3D Size { get; set; }
        public float Rotation { get; set; }
        public Color Color { get; set; }
    }

    class Size3D
    {
        public float Width { get; set; }
        public float Height { get; set; }
        public float Depth { get; set; }

        public Size3D(float width, float height, float depth)
        {
            Width = width;
            Height = height;
            Depth = depth;
        }
    }
}