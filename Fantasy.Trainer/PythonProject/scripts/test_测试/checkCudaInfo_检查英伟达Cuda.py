import torch

# 检查是否有可用的CUDA设备
print("CUDA是否可用:", torch.cuda.is_available())

# 如果CUDA可用，查看PyTorch支持的CUDA版本
if torch.cuda.is_available():
    print("PyTorch支持的CUDA版本:", torch.version.cuda)
    print("当前使用的GPU:", torch.cuda.get_device_name(0))
    print("当前GPU的计算能力:", torch.cuda.get_device_capability(0))
else:
    print("当前环境不支持CUDA")