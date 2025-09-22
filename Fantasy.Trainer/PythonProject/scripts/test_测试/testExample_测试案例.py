# %% 导入依赖库
import numpy as np
import random
from enum import Enum
import gymnasium as gym
from gymnasium import spaces
import torch
import torch.nn as nn
import torch.optim as optim

# %% 常量与枚举定义
# 小兵状态枚举
class SoldierState(Enum):
    HEALTHY = 0          # 健康：可战斗、可医疗
    INJURED = 1          # 受伤：需被护送至隐蔽点治疗
    CRITICALLY_INJURED = 2  # 重伤：仅缓慢移动，需救助，可能被俘虏
    CAPTURED = 3         # 被俘虏：退出当前战斗

# 地形类型
class TerrainType(Enum):
    PLAIN = 0            # 平原：移动速度1.0x
    HILL = 1             # 丘陵：移动速度0.7x
    OBSTACLE = 2         # 障碍物：不可通行
    HIDEOUT = 3          # 隐蔽点：仅此处可治疗，移动速度0.5x

# 战场与小兵参数
BATTLEFIELD_SIZE = (20, 20)  # 20x20网格战场
SOLDIERS_PER_TEAM = 10       # 每队10人
MAX_HEALTH = 100             # 小兵最大健康值
INJURED_THRESHOLD = 50       # 健康值<50→受伤
CRIT_INJURED_THRESHOLD = 20  # 健康值<20→重伤
ATTACK_RANGE = 2             # 攻击范围（网格距离）
ATTACK_DAMAGE = 15           # 基础攻击力
HEAL_RANGE = 1               # 治疗范围（仅在隐蔽点生效）
HEAL_AMOUNT = 10             # 每次治疗恢复量
MOVE_SPEED_BASE = 1          # 基础移动速度（格子/时间步）
CAPTURE_DISTANCE = 1         # 敌人距离<1时可俘虏重伤小兵
HIDEOUT_NUM = 4              # 战场隐蔽点数量（每队2个）

# %% 小兵类（Soldier）
class Soldier:
    def __init__(self, soldier_id, team_id, start_pos):
        self.soldier_id = soldier_id  # 小兵唯一ID
        self.team_id = team_id        # 所属队伍（0或1）
        self.pos = np.array(start_pos, dtype=int)  # 当前位置（x,y）
        self.max_health = MAX_HEALTH
        self.current_health = MAX_HEALTH  # 初始健康值满
        self.state = SoldierState.HEALTHY  # 初始状态健康
        self.move_speed = MOVE_SPEED_BASE  # 初始移动速度
        self.attack_cooldown = 0  # 攻击冷却（时间步）
        self.heal_cooldown = 0    # 治疗冷却（时间步）

    def update_state_by_health(self):
        """根据健康值更新小兵状态（核心状态切换逻辑）"""
        if self.current_health <= 0:
            self.state = SoldierState.CAPTURED  # 健康值归0视为被俘虏
        elif self.current_health < CRIT_INJURED_THRESHOLD:
            self.state = SoldierState.CRITICALLY_INJURED
            self.move_speed = MOVE_SPEED_BASE * 0.3  # 重伤：速度降为30%
        elif self.current_health < INJURED_THRESHOLD:
            self.state = SoldierState.INJURED
            self.move_speed = MOVE_SPEED_BASE * 0.6  # 受伤：速度降为60%
        else:
            self.state = SoldierState.HEALTHY
            self.move_speed = MOVE_SPEED_BASE  # 健康：速度恢复

    def take_damage(self, damage):
        """受到伤害：更新健康值并切换状态"""
        if self.state != SoldierState.CAPTURED:
            self.current_health = max(0, self.current_health - damage)
            self.update_state_by_health()

    def receive_heal(self, heal_amount):
        """接受治疗：仅在隐蔽点且非俘虏时生效"""
        if self.state != SoldierState.CAPTURED:
            self.current_health = min(self.max_health, self.current_health + heal_amount)
            self.update_state_by_health()

    def move(self, target_pos, battlefield):
        """移动逻辑：受地形和状态影响，不可穿墙"""
        if self.state == SoldierState.CAPTURED:
            return  # 俘虏不可移动

        # 计算目标方向（单位向量）
        dir = target_pos - self.pos
        if np.linalg.norm(dir) < 1e-5:
            return  # 已到达目标位置
        dir = dir / np.linalg.norm(dir)  # 归一化方向

        # 计算实际移动距离（受地形影响）
        terrain = battlefield[self.pos[0], self.pos[1]]
        speed_multiplier = {
            TerrainType.PLAIN.value: 1.0,
            TerrainType.HILL.value: 0.7,
            TerrainType.HIDEOUT.value: 0.5,
            TerrainType.OBSTACLE.value: 0.0  # 障碍物不可移动
        }[terrain]
        actual_speed = self.move_speed * speed_multiplier

        # 更新位置（网格整数坐标）
        new_pos = self.pos + dir * actual_speed
        new_pos = np.clip(new_pos, 0, BATTLEFIELD_SIZE[0]-1).astype(int)  # 边界裁剪
        if battlefield[new_pos[0], new_pos[1]] != TerrainType.OBSTACLE.value:
            self.pos = new_pos

    def is_in_hideout(self, battlefield):
        """判断是否在隐蔽点（治疗必需条件）"""
        return battlefield[self.pos[0], self.pos[1]] == TerrainType.HIDEOUT.value

# %% 战场环境类（BattleEnv）
class BattleEnv(gym.Env):
    metadata = {"render_modes": ["console"], "render_fps": 10}

    def __init__(self, render_mode=None):
        super().__init__()
        self.render_mode = render_mode
        self.create()  # 初始化环境（create方法：一次性创建战场静态元素）

        # 定义强化学习状态空间（多智能体：每队10个小兵，每个小兵6维状态）
        self.observation_space = spaces.Box(
            low=0, high=np.max([BATTLEFIELD_SIZE[0], BATTLEFIELD_SIZE[1], MAX_HEALTH, 3, 5, 5]),
            shape=(2 * SOLDIERS_PER_TEAM, 6), dtype=int
        )

        # 定义动作空间（每个小兵4类动作，离散动作）
        self.action_space = spaces.Tuple([
            spaces.Discrete(4) for _ in range(2 * SOLDIERS_PER_TEAM)
        ])

    def create(self):
        """环境创建方法：生成战场静态元素（地形、隐蔽点、障碍物），仅初始化一次"""
        # 1. 生成地形（默认平原，随机生成丘陵和障碍物）
        self.battlefield = np.full(BATTLEFIELD_SIZE, TerrainType.PLAIN.value, dtype=int)
        hill_ratio = 0.2  # 20%丘陵
        obstacle_ratio = 0.1  # 10%障碍物
        for i in range(BATTLEFIELD_SIZE[0]):
            for j in range(BATTLEFIELD_SIZE[1]):
                rand = random.random()
                if rand < obstacle_ratio:
                    self.battlefield[i, j] = TerrainType.OBSTACLE.value
                elif rand < obstacle_ratio + hill_ratio:
                    self.battlefield[i, j] = TerrainType.HILL.value

        # 2. 生成隐蔽点（每队2个，分别在战场两侧，避免初始冲突）
        self.hideouts = []
        # 队伍0隐蔽点：左侧区域（x∈[0,5]）
        for _ in range(HIDEOUT_NUM // 2):
            while True:
                pos = (random.randint(0, 5), random.randint(0, BATTLEFIELD_SIZE[1]-1))
                if self.battlefield[pos[0], pos[1]] != TerrainType.OBSTACLE.value:
                    self.battlefield[pos[0], pos[1]] = TerrainType.HIDEOUT.value
                    self.hideouts.append(pos)
                    break
        # 队伍1隐蔽点：右侧区域（x∈[14,19]）
        for _ in range(HIDEOUT_NUM // 2):
            while True:
                pos = (random.randint(14, 19), random.randint(0, BATTLEFIELD_SIZE[1]-1))
                if self.battlefield[pos[0], pos[1]] != TerrainType.OBSTACLE.value:
                    self.battlefield[pos[0], pos[1]] = TerrainType.HIDEOUT.value
                    self.hideouts.append(pos)
                    break

        # 3. 初始化小兵（后续reset会重置位置和状态，此处仅存结构）
        self.soldiers = []
        # 队伍0：初始位置左侧（x∈[1,3]）
        for i in range(SOLDIERS_PER_TEAM):
            start_pos = (random.randint(1, 3), random.randint(1, BATTLEFIELD_SIZE[1]-2))
            self.soldiers.append(Soldier(i, team_id=0, start_pos=start_pos))
        # 队伍1：初始位置右侧（x∈[16,18]）
        for i in range(SOLDIERS_PER_TEAM):
            start_pos = (random.randint(16, 18), random.randint(1, BATTLEFIELD_SIZE[1]-2))
            self.soldiers.append(Soldier(i + SOLDIERS_PER_TEAM, team_id=1, start_pos=start_pos))

    def reset(self, seed=None, options=None):
        """环境重置方法：重置小兵状态、位置，恢复战场动态元素（每次episode调用）"""
        super().reset(seed=seed)
        random.seed(seed)
        np.random.seed(seed)

        # 1. 重置小兵状态（健康值、状态、冷却）
        for soldier in self.soldiers:
            soldier.current_health = MAX_HEALTH
            soldier.state = SoldierState.HEALTHY
            soldier.attack_cooldown = 0
            soldier.heal_cooldown = 0
            # 重置位置（回到初始阵营区域）
            if soldier.team_id == 0:
                start_pos = (random.randint(1, 3), random.randint(1, BATTLEFIELD_SIZE[1]-2))
            else:
                start_pos = (random.randint(16, 18), random.randint(1, BATTLEFIELD_SIZE[1]-2))
            soldier.pos = np.array(start_pos, dtype=int)

        # 2. 重置战场统计信息
        self.episode_step = 0
        self.team0_captured = 0  # 队伍0被俘虏数
        self.team1_captured = 0  # 队伍1被俘虏数
        self.team0_kills = 0     # 队伍0击杀数
        self.team1_kills = 0     # 队伍1击杀数
        self.team0_heals = 0     # 队伍0治疗次数
        self.team1_heals = 0     # 队伍1治疗次数

        # 3. 返回初始观测
        observation = self._get_observation()
        info = self._get_info()
        return observation, info

    def step(self, actions):
        """环境步进方法：处理所有小兵动作，更新状态，计算奖励（核心交互逻辑）"""
        self.episode_step += 1
        reward = np.zeros(2 * SOLDIERS_PER_TEAM)  # 每个小兵单独奖励（多智能体）
        done = False

        # 1. 处理冷却时间（攻击/治疗冷却-1）
        for soldier in self.soldiers:
            if soldier.attack_cooldown > 0:
                soldier.attack_cooldown -= 1
            if soldier.heal_cooldown > 0:
                soldier.heal_cooldown -= 1

        # 2. 按动作类型处理每个小兵的行为
        for idx, (soldier, action) in enumerate(zip(self.soldiers, actions)):
            if soldier.state == SoldierState.CAPTURED:
                continue  # 俘虏不执行动作

            team_id = soldier.team_id
            ally_soldiers = [s for s in self.soldiers if s.team_id == team_id and s.state != SoldierState.CAPTURED]
            enemy_soldiers = [s for s in self.soldiers if s.team_id != team_id and s.state != SoldierState.CAPTURED]

            # 动作0：移动（上下左右随机选择一个方向）
            if action == 0:
                dirs = [(-1, 0), (1, 0), (0, -1), (0, 1)]  # 上下左右
                target_dir = random.choice(dirs)
                target_pos = soldier.pos + np.array(target_dir)
                soldier.move(target_pos, self.battlefield)
                reward[idx] += 0.1  # 移动奖励（鼓励探索战场）

            # 动作1：攻击（仅健康小兵可执行，冷却结束+在攻击范围）
            elif action == 1 and soldier.state == SoldierState.HEALTHY:
                if soldier.attack_cooldown > 0 or len(enemy_soldiers) == 0:
                    continue
                # 选择最近的敌人
                enemy_distances = [np.linalg.norm(soldier.pos - e.pos) for e in enemy_soldiers]
                nearest_enemy = enemy_soldiers[np.argmin(enemy_distances)]
                if enemy_distances[np.argmin(enemy_distances)] <= ATTACK_RANGE:
                    # 执行攻击
                    nearest_enemy.take_damage(ATTACK_DAMAGE)
                    soldier.attack_cooldown = 3  # 攻击冷却3步
                    # 奖励：攻击成功+击杀额外奖励
                    reward[idx] += 1.0  # 攻击奖励
                    if nearest_enemy.state == SoldierState.CAPTURED:
                        reward[idx] += 5.0  # 击杀奖励
                        if team_id == 0:
                            self.team0_kills += 1
                        else:
                            self.team1_kills += 1

            # 动作2：治疗（仅健康小兵在隐蔽点可执行，冷却结束+在治疗范围）
            elif action == 2 and soldier.state == SoldierState.HEALTHY:
                if soldier.heal_cooldown > 0 or not soldier.is_in_hideout(self.battlefield):
                    continue
                # 选择最近的受伤/重伤队友
                injured_allies = [a for a in ally_soldiers if a.state in [SoldierState.INJURED, SoldierState.CRITICALLY_INJURED]]
                if len(injured_allies) == 0:
                    continue
                ally_distances = [np.linalg.norm(soldier.pos - a.pos) for a in injured_allies]
                nearest_ally = injured_allies[np.argmin(ally_distances)]
                if ally_distances[np.argmin(ally_distances)] <= HEAL_RANGE:
                    # 执行治疗
                    nearest_ally.receive_heal(HEAL_AMOUNT)
                    soldier.heal_cooldown = 2  # 治疗冷却2步
                    reward[idx] += 2.0  # 治疗奖励（鼓励医疗协作）
                    if team_id == 0:
                        self.team0_heals += 1
                    else:
                        self.team1_heals += 1

            # 动作3：护送（仅健康小兵可执行，护送重伤队友到最近隐蔽点）
            elif action == 3 and soldier.state == SoldierState.HEALTHY:
                # 选择最近的重伤队友
                crit_ally = [a for a in ally_soldiers if a.state == SoldierState.CRITICALLY_INJURED]
                if len(crit_ally) == 0:
                    continue
                ally_distances = [np.linalg.norm(soldier.pos - a.pos) for a in crit_ally]
                target_ally = crit_ally[np.argmin(ally_distances)]
                # 第一步：移动到重伤队友身边
                if np.linalg.norm(soldier.pos - target_ally.pos) > 1:
                    soldier.move(target_ally.pos, self.battlefield)
                    reward[idx] += 0.5  # 靠近重伤队友奖励
                # 第二步：一起移动到最近的隐蔽点
                else:
                    # 找最近的隐蔽点
                    hideout_distances = [np.linalg.norm(target_ally.pos - np.array(h)) for h in self.hideouts]
                    nearest_hideout = self.hideouts[np.argmin(hideout_distances)]
                    # 护送者和重伤者一起向隐蔽点移动
                    soldier.move(np.array(nearest_hideout), self.battlefield)
                    target_ally.move(np.array(nearest_hideout), self.battlefield)
                    reward[idx] += 1.0  # 护送移动奖励
                    # 到达隐蔽点：额外奖励
                    if target_ally.is_in_hideout(self.battlefield):
                        reward[idx] += 3.0  # 护送成功奖励（避免被俘虏）

        # 3. 处理重伤小兵被俘虏逻辑
        for soldier in self.soldiers:
            if soldier.state != SoldierState.CRITICALLY_INJURED:
                continue
            # 检查是否有敌方健康小兵靠近
            enemy_healthy = [s for s in self.soldiers if s.team_id != soldier.team_id and s.state == SoldierState.HEALTHY]
            for enemy in enemy_healthy:
                if np.linalg.norm(soldier.pos - enemy.pos) <= CAPTURE_DISTANCE:
                    # 判定为被俘虏
                    soldier.state = SoldierState.CAPTURED
                    if soldier.team_id == 0:
                        self.team0_captured += 1
                        # 敌方小兵获得俘虏奖励
                        enemy_idx = self.soldiers.index(enemy)
                        reward[enemy_idx] += 4.0
                    else:
                        self.team1_captured += 1
                        enemy_idx = self.soldiers.index(enemy)
                        reward[enemy_idx] += 4.0
                    break

        # 4. 判断战斗是否结束（任一队伍被俘虏数≥8 或 步数≥200）
        if self.team0_captured >= 8 or self.team1_captured >= 8 or self.episode_step >= 200:
            done = True
            # 最终胜负奖励（团队奖励分配到每个小兵）
            if self.team0_captured < self.team1_captured:
                # 队伍0获胜：所有队伍0小兵+10奖励，队伍1-10
                reward[:SOLDIERS_PER_TEAM] += 10.0
                reward[SOLDIERS_PER_TEAM:] -= 10.0
            elif self.team1_captured < self.team0_captured:
                # 队伍1获胜：所有队伍1小兵+10奖励，队伍0-10
                reward[SOLDIERS_PER_TEAM:] += 10.0
                reward[:SOLDIERS_PER_TEAM] -= 10.0

        # 5. 返回下一步观测、奖励、结束标志、信息
        observation = self._get_observation()
        info = self._get_info()
        return observation, reward, done, False, info

    def _get_observation(self):
        """获取当前环境观测（多智能体状态）"""
        observation = []
        for soldier in self.soldiers:
            obs = [
                soldier.pos[0],
                soldier.pos[1],
                soldier.current_health,
                soldier.state.value,
                soldier.attack_cooldown,
                soldier.heal_cooldown
            ]
            observation.append(obs)
        return np.array(observation, dtype=int)

    def _get_info(self):
        """获取环境统计信息（用于调试和分析策略）"""
        return {
            "step": self.episode_step,
            "team0_captured": self.team0_captured,
            "team1_captured": self.team1_captured,
            "team0_kills": self.team0_kills,
            "team1_kills": self.team1_kills,
            "team0_heals": self.team0_heals,
            "team1_heals": self.team1_heals
        }

    def render(self):
        """控制台渲染战场（可视化当前状态）"""
        if self.render_mode != "console":
            return
        # 绘制战场网格
        grid = np.full(BATTLEFIELD_SIZE, ".", dtype=str)
        # 标记障碍物
        for i in range(BATTLEFIELD_SIZE[0]):
            for j in range(BATTLEFIELD_SIZE[1]):
                if self.battlefield[i, j] == TerrainType.OBSTACLE.value:
                    grid[i, j] = "X"
                elif self.battlefield[i, j] == TerrainType.HIDEOUT.value:
                    grid[i, j] = "H"
        # 标记小兵（0=队伍0健康，0'=队伍0受伤，0''=队伍0重伤；1同理）
        for soldier in self.soldiers:
            x, y = soldier.pos
            if soldier.state == SoldierState.CAPTURED:
                continue
            elif soldier.state == SoldierState.HEALTHY:
                grid[x, y] = str(soldier.team_id)
            elif soldier.state == SoldierState.INJURED:
                grid[x, y] = f"{soldier.team_id}'"
            elif soldier.state == SoldierState.CRITICALLY_INJURED:
                grid[x, y] = f"{soldier.team_id}''"
        # 打印网格和统计信息
        print("=" * 50)
        print(f"Step: {self.episode_step} | Team0 Captured: {self.team0_captured} | Team1 Captured: {self.team1_captured}")
        print(f"Team0 Kills: {self.team0_kills} | Team1 Kills: {self.team1_kills} | Heals: {self.team0_heals}/{self.team1_heals}")
        for row in grid:
            print(" ".join(row))
        print("=" * 50)

# %% QMIX多智能体算法
class QMixNet(nn.Module):
    """QMIX集中式价值网络（解决多智能体信用分配问题）"""
    def __init__(self, obs_dim, n_agents):
        super().__init__()
        self.n_agents = n_agents
        # 每个智能体的Q网络
        self.agent_q_net = nn.Sequential(
            nn.Linear(obs_dim, 64),
            nn.ReLU(),
            nn.Linear(64, 64),
            nn.ReLU(),
            nn.Linear(64, 4)  # 4个动作
        )
        # 混合网络（将个体Q值混合为团队Q值）
        self.mix_net = nn.Sequential(
            nn.Linear(n_agents * 4, 64),
            nn.ReLU(),
            nn.Linear(64, 1)
        )

    def forward(self, obs):
        """obs: (batch_size, n_agents, obs_dim)"""
        batch_size = obs.shape[0]
        # 1. 计算每个智能体的个体Q值（batch_size, n_agents, 4）
        agent_q = self.agent_q_net(obs.reshape(-1, obs.shape[-1])).reshape(batch_size, self.n_agents, 4)
        # 2. 混合为团队Q值（batch_size, 1）
        team_q = self.mix_net(agent_q.reshape(batch_size, -1))
        return agent_q, team_q


def train_qmix(env, n_episodes=1000, lr=1e-4, gamma=0.95, batch_size=32, buffer_size=10000):
    """QMIX算法训练逻辑"""
    n_agents = 2 * SOLDIERS_PER_TEAM
    obs_dim = 6  # 每个智能体6维观测
    # 初始化网络和优化器
    q_net = QMixNet(obs_dim, n_agents)
    target_q_net = QMixNet(obs_dim, n_agents)
    target_q_net.load_state_dict(q_net.state_dict())  # 目标网络初始同步
    optimizer = optim.Adam(q_net.parameters(), lr=lr)
    loss_fn = nn.MSELoss()

    # 经验回放缓冲区（存储：obs, actions, rewards, next_obs, dones）
    replay_buffer = []

    for episode in range(n_episodes):
        obs, _ = env.reset()
        total_reward = 0
        done = False

        while not done:
            # 1. 选择动作（ε-贪婪策略，ε随episode衰减）
            eps = max(0.1, 0.9 - episode / n_episodes * 0.8)  # ε从0.9衰减到0.1
            if random.random() < eps:
                actions = [env.action_space[i].sample() for i in range(n_agents)]
            else:
                # 网络预测动作（选择Q值最大的动作）
                obs_tensor = torch.FloatTensor(obs).unsqueeze(0)  # (1, n_agents, obs_dim)
                agent_q, _ = q_net(obs_tensor)
                actions = agent_q.argmax(dim=-1).squeeze(0).tolist()

            # 2. 执行动作，获取下一步信息
            next_obs, rewards, done, _, info = env.step(actions)
            total_reward += sum(rewards)

            # 3. 存储经验到缓冲区
            replay_buffer.append((obs, actions, rewards, next_obs, done))
            if len(replay_buffer) > buffer_size:
                replay_buffer.pop(0)  # 缓冲区满则删除最早经验

            # 4. 经验回放更新网络
            if len(replay_buffer) >= batch_size:
                # 随机采样批次经验
                batch = random.sample(replay_buffer, batch_size)
                obs_batch = torch.FloatTensor([b[0] for b in batch])  # (batch_size, n_agents, obs_dim)
                action_batch = torch.LongTensor([b[1] for b in batch])  # (batch_size, n_agents)
                reward_batch = torch.FloatTensor([b[2] for b in batch])  # (batch_size, n_agents)
                next_obs_batch = torch.FloatTensor([b[3] for b in batch])  # (batch_size, n_agents, obs_dim)
                done_batch = torch.FloatTensor([b[4] for b in batch])  # (batch_size, 1)

                # 计算目标Q值（target_q = reward + gamma * max_target_team_q）
                with torch.no_grad():
                    next_agent_q, next_team_q = target_q_net(next_obs_batch)
                    target_q = reward_batch.mean(dim=1, keepdim=True) + gamma * next_team_q * (1 - done_batch.unsqueeze(1))

                # 计算当前Q值
                agent_q, team_q = q_net(obs_batch)
                # 提取每个智能体对应动作的Q值（batch_size, n_agents）
                action_indices = action_batch.unsqueeze(-1).expand(-1, -1, 4).argmax(dim=-1).unsqueeze(-1)
                selected_agent_q = agent_q.gather(dim=-1, index=action_indices).squeeze(-1)
                # 混合为当前团队Q值（与目标Q值维度匹配）
                current_team_q = team_q

                # 计算损失并更新
                loss = loss_fn(current_team_q, target_q)
                optimizer.zero_grad()
                loss.backward()
                optimizer.step()

            # 5. 更新观测
            obs = next_obs

            # 6. 每10步渲染一次（可选）
            if episode % 100 == 0 and env.render_mode == "console":
                env.render()

        # 7. 每100个episode更新目标网络
        if episode % 100 == 0:
            target_q_net.load_state_dict(q_net.state_dict())

        # 8. 打印训练日志
        if (episode + 1) % 50 == 0:
            print(f"Episode: {episode+1:4d} | Total Reward: {total_reward:6.2f} | "
                  f"Team0 Captured: {info['team0_captured']} | Team1 Captured: {info['team1_captured']} | "
                  f"Team0 Heals: {info['team0_heals']} | Team1 Heals: {info['team1_heals']}")

    # 保存训练好的模型
    torch.save(q_net.state_dict(), "battle_qmix_model.pth")
    print("训练完成！模型已保存为 battle_qmix_model.pth")

# %% 启动训练
if __name__ == "__main__":
    # 初始化环境（开启控制台渲染）
    env = BattleEnv(render_mode="console")
    # 启动QMIX训练（1000个episode，可调整）
    train_qmix(env, n_episodes=1000)
    env.close()
