using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;
using static Raylib_cs.Raylib;

namespace XTouhou;

class Program
{
    public const int ScreenWidth = 800;
    public const int ScreenHeight = 600;
    
    enum GameState { TITLE, PLAYING, GAME_OVER }
    static GameState gameState = GameState.TITLE;
    
    // 玩家相关
    public static Player player = new Player();
    
    // 对象池
    public static List<PlayerBullet> playerBullets = new List<PlayerBullet>();
    static List<Enemy> enemies = new List<Enemy>();
    public static List<EnemyBullet> enemyBullets = new List<EnemyBullet>();

    static void Main()
    {
        InitWindow(ScreenWidth, ScreenHeight, "东方弹幕游戏");
        SetTargetFPS(60);

        player = new Player();
        InitObjectPools();

        while (!WindowShouldClose())
        {
            float deltaTime = GetFrameTime();
            
            switch (gameState)
            {
                case GameState.TITLE:
                    UpdateTitleScreen();
                    break;
                case GameState.PLAYING:
                    UpdateGame(deltaTime);
                    break;
                case GameState.GAME_OVER:
                    UpdateGameOver();
                    break;
            }
            
            BeginDrawing();
            ClearBackground(Color.Black);
            
            switch (gameState)
            {
                case GameState.TITLE:
                    DrawTitleScreen();
                    break;
                case GameState.PLAYING:
                    DrawGame();
                    break;
                case GameState.GAME_OVER:
                    DrawGameOver();
                    break;
            }
            
            EndDrawing();
        }
        
        CloseWindow();
    }

    static void InitObjectPools()
    {
        // 初始化玩家子弹池
        for (int i = 0; i < 200; i++)
            playerBullets.Add(new PlayerBullet());
        
        // 初始化敌机池
        for (int i = 0; i < 50; i++)
            enemies.Add(new Enemy());
        
        // 初始化敌弹池
        for (int i = 0; i < 1000; i++)
            enemyBullets.Add(new EnemyBullet());
    }

    static void UpdateTitleScreen()
    {
        if (IsKeyPressed(KeyboardKey.Enter))
        {
            StartNewGame();
            gameState = GameState.PLAYING;
        }
    }

    static void StartNewGame()
    {
        player.Reset();
        
        // 重置所有对象
        playerBullets.ForEach(b => b.active = false);
        enemies.ForEach(e => e.active = false);
        enemyBullets.ForEach(b => b.active = false);
    }

    static void UpdateGame(float deltaTime)
    {
        // 玩家控制
        player.Update(deltaTime);
        
        // 玩家射击
        if (IsKeyDown(KeyboardKey.Z))
            player.Shoot(deltaTime);
        
        // 慢速移动（Focus模式）
        player.isFocused = IsKeyDown(KeyboardKey.LeftShift);
        
        // 敌机生成逻辑
        SpawnEnemies(deltaTime);
        
        // 更新所有实体
        UpdateEntities(deltaTime);
        
        // 碰撞检测
        CheckCollisions();
    }

    static float spawnTimer = 0;

    static void SpawnEnemies(float deltaTime)
    {
        // 简单敌机生成逻辑
        spawnTimer += deltaTime;
        
        if (spawnTimer >= 1.0f) // 每秒生成一个
        {
            spawnTimer = 0;
            Enemy enemy = enemies.Find(e => !e.active);
            if (enemy != null)
            {
                enemy.Spawn(new Vector2(
                    GetRandomValue(100, ScreenWidth - 100),
                    -50
                ));
            }
        }
    }

    static void UpdateEntities(float deltaTime)
    {
        // 更新玩家子弹
        foreach (var bullet in playerBullets)
        {
            if (bullet.active)
                bullet.Update(deltaTime);
        }
        
        // 更新敌人
        foreach (var enemy in enemies)
        {
            if (enemy.active)
            {
                enemy.Update(deltaTime);
                enemy.Shoot(deltaTime); // 敌机射击
            }
        }
        
        // 更新敌弹
        foreach (var bullet in enemyBullets)
        {
            if (bullet.active)
                bullet.Update(deltaTime);
        }
    }

    static void CheckCollisions()
    {
        // 玩家子弹 vs 敌人
        foreach (var bullet in playerBullets)
        {
            if (!bullet.active) continue;
            
            foreach (var enemy in enemies)
            {
                if (enemy.active && CheckCollisionCircles(
                        bullet.position, bullet.radius,
                        enemy.position, enemy.radius))
                {
                    bullet.active = false;
                    enemy.TakeDamage(1);
                }
            }
        }
        
        // 敌弹 vs 玩家
        foreach (var bullet in enemyBullets)
        {
            if (bullet.active && CheckCollisionCircles(
                    bullet.position, bullet.radius,
                    player.position, player.hitRadius))
            {
                // 玩家中弹处理
                gameState = GameState.GAME_OVER;
            }
        }
    }

    static void DrawTitleScreen()
    {
        DrawText("东方弹幕游戏", ScreenWidth/2 - 100, 150, 40, Color.White);
        DrawText("按 ENTER 开始游戏", ScreenWidth/2 - 120, 300, 24, Color.Green);
        DrawText("Z: 射击  Shift: 低速移动  方向键: 移动", ScreenWidth/2 - 200, 400, 20, Color.Yellow);
    }

    static void DrawGame()
    {
        // 绘制玩家
        player.Draw();
        
        // 绘制玩家子弹
        foreach (var bullet in playerBullets)
            if (bullet.active) bullet.Draw();
        
        // 绘制敌人
        foreach (var enemy in enemies)
            if (enemy.active) enemy.Draw();
        
        // 绘制敌弹
        foreach (var bullet in enemyBullets)
            if (bullet.active) bullet.Draw();
        
        // 绘制UI
        DrawText($"Score: {player.score}", 10, 10, 20, Color.White);
        DrawText($"Power: {player.power}", 10, 40, 20, Color.Yellow);
    }

    static void DrawGameOver()
    {
        DrawText("游戏结束", ScreenWidth/2 - 80, ScreenHeight/2 - 50, 40, Color.Red);
        DrawText($"最终得分: {player.score}", ScreenWidth / 2 - 100, ScreenHeight / 2, 30, Color.White);
        DrawText("按 ENTER 重新开始", ScreenWidth/2 - 120, ScreenHeight/2 + 80, 24, Color.Green);
    }

    static void UpdateGameOver()
    {
        if (IsKeyPressed(KeyboardKey.Enter))
        {
            StartNewGame();
            gameState = GameState.TITLE;
        }
    }
}

// ==== 实体类 ====
class Player
{
    public Vector2 position;
    public float speed = 300;
    public float hitRadius = 5;
    public bool isFocused = false;
    public int score = 0;
    public float power = 1.0f;
    
    private float shootCooldown = 0;
    private const float SHOOT_INTERVAL = 0.1f;

    public Player()
    {
        Reset();
    }

    public void Reset()
    {
        position = new Vector2(400, 500);
        score = 0;
        power = 1.0f;
    }

    public void Update(float deltaTime)
    {
        // 移动输入
        Vector2 input = new Vector2(
            IsKeyDown(KeyboardKey.Right) ? 1 : IsKeyDown(KeyboardKey.Left) ? -1 : 0,
            IsKeyDown(KeyboardKey.Down) ? 1 : IsKeyDown(KeyboardKey.Up) ? -1 : 0
        );
        
        // 标准化移动向量
        if (input.LengthSquared() > 0)
            input = Vector2.Normalize(input);
        
        // Focus模式减速
        float moveSpeed = isFocused ? speed * 0.4f : speed;
        
        // 更新位置
        position += input * moveSpeed * deltaTime;
        
        // 屏幕边界检查
        position.X = Math.Clamp(position.X, 10, Program.ScreenWidth - 10);
        position.Y = Math.Clamp(position.Y, 10, Program.ScreenHeight - 10);
        
        // 射击冷却
        if (shootCooldown > 0)
            shootCooldown -= deltaTime;
    }

    public void Shoot(float deltaTime)
    {
        if (shootCooldown <= 0)
        {
            // 创建子弹
            for (int i = 0; i < 2; i++) // 双发
            {
                PlayerBullet bullet = Program.playerBullets.Find(b => !b.active);
                if (bullet != null)
                {
                    float offset = i == 0 ? -5 : 5;
                    bullet.Activate(
                        position + new Vector2(offset, -15),
                        new Vector2(0, -600)
                    );
                }
            }
            
            shootCooldown = SHOOT_INTERVAL;
        }
    }

    public void Draw()
    {
        // 绘制玩家
        DrawCircleV(position, 8, isFocused ? Color.SkyBlue : Color.White);
        
        // 绘制判定点
        DrawCircleV(position, hitRadius, Color.Red);
    }
}

class PlayerBullet
{
    public Vector2 position;
    public Vector2 velocity;
    public bool active;
    public float radius = 3;
    
    public void Activate(Vector2 pos, Vector2 vel)
    {
        position = pos;
        velocity = vel;
        active = true;
    }
    
    public void Update(float deltaTime)
    {
        position += velocity * deltaTime;
        
        // 超出屏幕则禁用
        if (position.Y < -20 || position.X < -20 || 
            position.X > Program.ScreenWidth + 20)
            active = false;
    }
    
    public void Draw()
    {
        DrawCircleV(position, radius, Color.Yellow);
    }
}

class Enemy
{
    public Vector2 position;
    public Vector2 velocity;
    public bool active;
    public float radius = 20;
    public int health = 10;
    
    private float shootTimer = 0;
    
    public void Spawn(Vector2 pos)
    {
        position = pos;
        velocity = new Vector2(0, 50);
        active = true;
        health = 10;
    }
    
    public void Update(float deltaTime)
    {
        position += velocity * deltaTime;
        
        // 简单移动模式
        if (position.Y > 150)
            velocity.Y = 0;
        
        // 超出屏幕则禁用
        if (position.Y > Program.ScreenHeight + 50)
            active = false;
    }
    
    public void Shoot(float deltaTime)
    {
        shootTimer += deltaTime;
        
        if (shootTimer >= 1.5f) // 每1.5秒射击一次
        {
            shootTimer = 0;
            
            // 创建弹幕 (简单圆形弹)
            for (int i = 0; i < 8; i++)
            {
                EnemyBullet bullet = Program.enemyBullets.Find(b => !b.active);
                if (bullet != null)
                {
                    float angle = i * (MathF.PI * 2 / 8);
                    Vector2 dir = new Vector2(MathF.Cos(angle), MathF.Sin(angle));
                    bullet.Activate(position, dir * 150);
                }
            }
        }
    }
    
    public void TakeDamage(int damage)
    {
        health -= damage;
        if (health <= 0)
        {
            active = false;
            // 增加玩家分数
            Program.player.score += 100;
        }
    }
    
    public void Draw()
    {
        DrawCircleV(position, radius, Color.Red);
    }
}

class EnemyBullet
{
    public Vector2 position;
    public Vector2 velocity;
    public bool active;
    public float radius = 4;
    
    public void Activate(Vector2 pos, Vector2 vel)
    {
        position = pos;
        velocity = vel;
        active = true;
    }
    
    public void Update(float deltaTime)
    {
        position += velocity * deltaTime;
        
        // 超出屏幕则禁用
        if (position.Y > Program.ScreenHeight + 20 || 
            position.X < -20 || position.X > Program.ScreenWidth + 20)
            active = false;
    }
    
    public void Draw()
    {
        DrawCircleV(position, radius, Color.Blue);
    }
}