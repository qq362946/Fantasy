import os
import subprocess
import sys

def export_conda_env(env_path: str, output_file: str = "condaEnv.yml") -> None:
    try:
        # 1. 导出环境指令
        command = f"conda env export -p {env_path} > {output_file}"
        result = subprocess.run(
            command,
            shell=True,
            check=True,
            capture_output=True,
            text=True
        )
        
        # 2. 读取导出的文件，删除包含prefix的行
        with open(output_file, "r", encoding="utf-8") as f:
            lines = f.readlines()
        
        # 过滤掉prefix行（保留其他所有内容）
        filtered_lines = [line for line in lines if not line.strip().startswith("prefix:")]
        
        # 3. 将过滤后的内容写回文件（覆盖原文件）
        with open(output_file, "w", encoding="utf-8") as f:
            f.writelines(filtered_lines)
        
        print(f"✅ 成功导出环境配置到 {output_file}（已自动移除prefix）")
        print(f"环境路径: {env_path}")
        
    except subprocess.CalledProcessError as e:
        print(f"❌ 导出失败: 命令执行出错")
        print(f"错误信息: {e.stderr}")
        sys.exit(1)
    except Exception as e:
        print(f"❌ 发生未知错误: {str(e)}")
        sys.exit(1)

if __name__ == "__main__":
    # 环境实际路径
    CONDA_ENV_PATH = "F:\\Unity\\Fantasy\\Fantasy-Fork\\Fantasy.Trainer\\PythonProject\\.conda" 
    
    export_conda_env(CONDA_ENV_PATH)
