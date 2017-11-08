using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using Microsoft.Kinect;

namespace KinPong
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class Game1 : Microsoft.Xna.Framework.Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;

        SpriteFont font;

        //Kinect
        KinectSensor kinect;
        //Funciona o esqueleto no kinect
        Skeleton[] skeletons;
        Skeleton p1;//Jogador 1 detectado no kinect
        Skeleton p2;//Jogador 2 detectado no kinect

        //Jogador
        float p1Posicao;
        float p2Posicao;
        clsObjeto p1Jogador, p2Jogador;//barras do jogador
        int p1Ponto = 0, p2Ponto = 0;//pontos
        int p2CPUVelocidade = 1;
        bool p1Teclado = false, p2Teclado = false;
        bool p1Detectado = false, p2Detectado = false;

        //Jogo - Cenário
        int jogoCena = 0;
        clsObjeto jogoCenarioLinhaCentral;
        double jogoTempo = 0;

        //Jogo - Bola
        int jogoBolaPosX = -1, jogoBolaPosY = -1, jogoBolaVelocidade = 5;
        clsObjeto jogoBola;

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            graphics.PreferredBackBufferWidth = 800;
            graphics.PreferredBackBufferHeight = 600;
            //graphics.IsFullScreen = true;

            Content.RootDirectory = "Content";
        }

        protected override void Initialize()
        {
            base.Initialize();
        }

        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);

            //Inicializa o sensor
            try
            {
                kinect = KinectSensor.KinectSensors[0];
                //Inicializa o esqueleto
                kinect.SkeletonStream.Enable();
                //Inicia tudo
                kinect.Start();

                kinect.SkeletonFrameReady += new EventHandler<SkeletonFrameReadyEventArgs>(kinect_SkeletonFrameReady);
            }
            catch { }

            // Carrega a font para escrita 
            font = Content.Load<SpriteFont>("gameFont");

            //carrega os objetos da tela
            p1Jogador = new clsObjeto(this, Content.Load<Texture2D>("barra"), new Vector2(50f, 500f), new Vector2(10f, 65f), graphics.PreferredBackBufferWidth, graphics.PreferredBackBufferHeight);
            p2Jogador = new clsObjeto(this, Content.Load<Texture2D>("barra"), new Vector2(780f, 300f), new Vector2(10f, 65f), graphics.PreferredBackBufferWidth, graphics.PreferredBackBufferHeight);
            jogoBola = new clsObjeto(this, Content.Load<Texture2D>("barra"), new Vector2(750f, 400f), new Vector2(15f, 15f), graphics.PreferredBackBufferWidth, graphics.PreferredBackBufferHeight);
            jogoCenarioLinhaCentral = new clsObjeto(this, Content.Load<Texture2D>("barra"), new Vector2(0, 399), new Vector2(2f, 600f), graphics.PreferredBackBufferWidth, graphics.PreferredBackBufferHeight);
            jogoCenarioLinhaCentral.position = new Vector2(399, 0);
        }

        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
        }

        protected override void OnExiting(Object sender, EventArgs args)
        {
            base.OnExiting(sender, args);
            if (kinect !=null) {
                kinect.Stop();
            }

            
        }

        void jogoIniciar()
        {
            jogoCena = 1;
        }

        void jogoJogadorPosicao()
        {
            KeyboardState ks = Keyboard.GetState();


            //Se for jogar pelo teclado, a tecla 1 ativa o jogador 1 e a tecla 2 o jogador 2. ENTER inicia
            if (ks.IsKeyDown(Keys.Q))
            {
                p1Jogador.position = new Vector2(5, (int)(p1Jogador.position.Y )-10);
            }
            else if (ks.IsKeyDown(Keys.A))
            {
                p1Jogador.position = new Vector2(5, (int)(p1Jogador.position.Y) + 10);
            }



            //Jogador 1
            if (p1 != null)
            {
                //muda a posição do jogador de acordo com a mão tendo como base o ombro esquerdo
                p1Posicao = jogoJogadorPosicaoCalcular(p1.Joints[JointType.ShoulderLeft].Position.Y - p1.Joints[JointType.HandRight].Position.Y);
                p1Jogador.position = new Vector2(5, (int)(p1Posicao));
            }

            //Verifica quem vai jogar: Jogador 2 ou CPU
            if (p2 != null)
            {
                //Jogador 2 foi detectado pelo kinect
                //
                //muda a posição do jogador de acordo com a mão
                p2Posicao = jogoJogadorPosicaoCalcular(p2.Joints[JointType.ShoulderLeft].Position.Y - p2.Joints[JointType.HandRight].Position.Y);
                p2Jogador.position = new Vector2(p2Jogador.position.X, (int)(p2Posicao));
            }
            
            else
            {
                //CPU joga
                if (p2Jogador.position.Y < 0)
                {
                    p2Jogador.position = new Vector2(p2Jogador.position.X, 0);
                }
                else if (p2Jogador.position.Y + p2Jogador.size.Y > graphics.PreferredBackBufferHeight)
                {
                    p2Jogador.position = new Vector2(p2Jogador.position.X, graphics.PreferredBackBufferHeight - p2Jogador.size.Y);
                }
                else
                {
                    //Sobe ou desce o oponente de acordo com a bola
                    if (jogoBolaPosY > 0)
                    {
                        p2Jogador.position = new Vector2(p2Jogador.position.X, p2Jogador.position.Y + p2CPUVelocidade);
                    }
                    else
                    {
                        p2Jogador.position = new Vector2(p2Jogador.position.X, p2Jogador.position.Y - p2CPUVelocidade);
                    }
                }

            }
        }

        int jogoJogadorPosicaoCalcular(float pos)
        {
            //Nota: 400 é a metade da tela
            pos = (pos * -1) * 400;
            
            if (pos > 0)// > 0  = mão para cima
            {
                //3= um valor para equilibrar a mão para não precisar esticar muito o braço
                pos = pos * 3;
                //300 - pos = o mais alto começa no zero
                pos = 300 - pos;
            }
            else if (pos < 0)// < 0  = mão para baixo
            {
                //pos é negativo mas neste caso precisa colocar como positivo porque para baixo é positivo
                pos = pos * -1;
                //3 = um valor para equilibrar a mão para não precisar esticar muito o braço
                pos = pos * 3;
                //soma 300 para descer. 
                pos = pos + 300;
            }
            return (int)pos;
        }

        void jogoBolaPosicao()
        {
            //** Bola
            //muda a bola de posição
            if (jogoBola.position.X < 0)
            {
                jogoBolaPosX = 1;
                p2Ponto++;
            }
            else if ((jogoBola.position.X + jogoBola.size.X) > graphics.PreferredBackBufferWidth)
            {
                jogoBolaPosX = -1;
                p1Ponto++;
                //aumenta velocidade do oponente para ficar mais difícil
                if (p1Ponto == 2)
                    p2CPUVelocidade++;
                else if (p1Ponto == 4)
                    p2CPUVelocidade++;
                else if (p1Ponto > 4)
                    p2CPUVelocidade++;
            }

            if (jogoBola.position.Y < 0)
            {
                jogoBolaPosY = 1;
            }
            else if ((jogoBola.position.Y + jogoBola.size.Y) > graphics.PreferredBackBufferHeight)
            {
                jogoBolaPosY = -1;
            }

            //Verifica a colisão da BOLA no JOGADOR
            if (jogoBola.ObjetoColisao(p1Jogador))
            {
                //faz a bola ir para a direita
                jogoBolaPosX = 1;
            }

            //Verifica a colisão da BOLA no OPONENTE
            if (jogoBola.ObjetoColisao(p2Jogador))
            {
                //faz a bola ir para a esquerda
                jogoBolaPosX = -1;
            }

            //muda a posição da bola
            jogoBola.position = new Vector2(jogoBola.position.X + (jogoBolaPosX * jogoBolaVelocidade), jogoBola.position.Y + (jogoBolaPosY * jogoBolaVelocidade));
        }

        bool jogoAguardarJogadores()
        {
            KeyboardState ks = Keyboard.GetState();


            //Se for jogar pelo teclado, a tecla 1 ativa o jogador 1 e a tecla 2 o jogador 2. ENTER inicia
            if (ks.IsKeyDown(Keys.D1))
            {
                p1Teclado = true;
                p1Detectado = true;
            }
            else if (ks.IsKeyDown(Keys.D2))
            {
                p2Teclado = true;
                p2Detectado = true;
            }

            //Verifica se vai jogar pelo teclado ou kinect. 
            //Se pressionou a tecla 1 ou 2, então será pelo teclado
            if (p1Teclado == true || p2Teclado == true)
            {

                if (ks.IsKeyDown(Keys.Enter))
                {
                    return true;
                }
            }
            else
            {
                //Verifica se o jogador 1 foi detectado pelo Kinect
                if (p1 != null)
                {
                    p1Detectado = true;
                    //Verifica se o jogador 2 foi detectado pelo Kinect
                    if (p2 != null)
                    {
                        p2Detectado = true;
                        //2 jogadores estão detectados. Para iniciar os 2 devem estar com a mão acima da cabeça
                        if ((p1.Joints[JointType.HandRight].Position.Y > p1.Joints[JointType.Head].Position.Y) && (p2.Joints[JointType.HandRight].Position.Y > p2.Joints[JointType.Head].Position.Y))
                        {
                            return true;
                        }
                    }
                    else
                    {
                        p2Detectado = false;
                        //Apenas o jogador 1 foi detectado pelo kinect. Se ele erguer a mão acima da cabeça, ele vai jogar com a CPU
                        if (p1.Joints[JointType.HandRight].Position.Y > p1.Joints[JointType.Head].Position.Y)
                        {
                            return true;
                        }
                    }
                }
                else
                {
                    p1Detectado = false;
                    p2Detectado = false;
                }
            }
            return false;
        }

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
                this.Exit();

            

            if (jogoCena == 0)
            {
                if (jogoAguardarJogadores() == true)
                {
                    //Inicia o jogo
                    jogoCena = 1;
                }
            }
            else if (jogoCena == 1)
            {
                jogoJogadorPosicao();
                jogoBolaPosicao();
            }

            base.Update(gameTime);

            jogoTempo += gameTime.ElapsedGameTime.TotalMinutes;
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);
            graphics.GraphicsDevice.Clear(Color.Black);
            // TODO: Add your drawing code here
            spriteBatch.Begin();
            
            if (jogoCena == 0)//menu
            {
                spriteBatch.DrawString(font, "KinPong", new Vector2(350, 50), Color.Yellow);

                if (p1Detectado == true)
                {
                    spriteBatch.DrawString(font, "Olá, jogador 1!", new Vector2(50, 150), Color.Turquoise);
                }
                if (p2Detectado == true)
                {
                    spriteBatch.DrawString(font, "Olá, jogador 2!", new Vector2(450, 150), Color.Turquoise);
                }
                if (kinect == null)
                {
                    spriteBatch.DrawString(font, "KINECT NÃO DETECTADO", new Vector2(250, 100), Color.Red);
                }

                //spriteBatch.DrawString(font, jogoTempo.ToString(), new Vector2(20, 300), Color.Blue);
                
                spriteBatch.DrawString(font, "Mão para cima para começar o jogo!", new Vector2(50, 200), Color.Turquoise);

                spriteBatch.DrawString(font, "-Objetivo: nao deixar a bolinha bater na sua parede", new Vector2(100, 500), Color.White);
                spriteBatch.DrawString(font, "-Como jogar: Use a mao direita e movimente para cima e para baixo", new Vector2(100, 530), Color.White);
            }
            else if (jogoCena == 1)//jogando
            {
                //jogador
                p1Jogador.Draw(spriteBatch);
                jogoBola.Draw(spriteBatch);
                p2Jogador.Draw(spriteBatch);

                //cenário
                jogoCenarioLinhaCentral.Draw(spriteBatch);
                spriteBatch.DrawString(font, p1Ponto.ToString(), new Vector2(355, 20), Color.Yellow);
                spriteBatch.DrawString(font, p2Ponto.ToString(), new Vector2(430, 20), Color.Yellow);
                //spriteBatch.DrawString(font, "diferenca: " + pos.ToString(), new Vector2(430, 50), Color.White);
                //spriteBatch.DrawString(font, "mao:" + _jMaoDireitaY.ToString(), new Vector2(430, 70), Color.White);
                //spriteBatch.DrawString(font, "ombro:" + p1OmbroDireitoY.ToString(), new Vector2(430, 90), Color.White);
            }
            spriteBatch.End();

            base.Draw(gameTime);
        }


        void kinect_SkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            using (SkeletonFrame skeletonFrame = e.OpenSkeletonFrame())
            {
                if (skeletonFrame != null)
                {
                    if (skeletons == null)
                    {
                        skeletons = new Skeleton[skeletonFrame.SkeletonArrayLength];
                    }

                    skeletonFrame.CopySkeletonDataTo(skeletons);

                    p1 = skeletons.Where(s => s.TrackingState == SkeletonTrackingState.Tracked).FirstOrDefault();
                    if (p1 != null)
                    {
                        //Detecta o segundo jogador e verifica o ID. Se o ID é igual, então o jogador 2 não está
                        //jogando.
                        p2 = skeletons.Where(s => s.TrackingState == SkeletonTrackingState.Tracked).LastOrDefault();
                        if (p1.TrackingId == p2.TrackingId)
                        {
                            //Coloca null para que o jogo não detecte o segundo jogador sendo o jogador 1()
                            p2 = null;
                        }
                    }
                }
            }
        }

    }
}
