# PVEPassthroughMonitork
一个可以自动扫描pve直通状态并且自动归还直通硬件的小工具（比如显卡）

## 编译和运行
使用.NET 7（控制台）编写并且在项目里面开启了 AOT， 编译成的文可以不依赖.NET 框架运行

## 使用
这个软件有两个参数：

1. --make_snapshot 创建快照

2. --auto_check 根据快照自动归还硬件

## 使用方法：  

1. 确认使用root运行（PVE默认是root，如果启用了sudo则需要sudo）    

2. 在不直通任何硬件且驱动完善的情况下，带 --make_snapshot 运行一次  

3. 带 --auto_check 参数运行程序并保持后台

## 注意事项：

1. 程序目录下生成 dev_snapshot 文件，这个文件就是软件带 --make_snapshot 运行的结果，请勿删除

2. 运行 --auto_check 时程序会阻塞当前控制台窗口，保留后台建议写 service 文件或者 nohup  

3. 保险起见，无论使用脚本运行或者直接输入命令，请cd到程序目录再运行

4. 如果你的PVE有桌面，你仍然需要hook虚拟机启动和停止，但是你只需要负责启动和停止桌面环境

5. 在 Release 里面我提供了 x64 版本的执行文件，这个文件有点大（.NET的原因）但是不需要.NET环境

## 测试情况：
PVE 7.5 + Win 11 直通 GT1030 测试通过，基本无bug，其他版本情况略有不同，如果出现bug可以和我联系

## 联系方式

1. QQ：1103660629

2. Email：1103660629@qq.com

## 碎碎念

给自己部署PVE服务器准备的东西

## 灵感来源

https://github.com/HelloZhing/pvevm-hooks

本人是在理解这个项目提供的脚本代码的基础上，自己扩写并在自己的环境上测试观察完成的（原谅我不会python和shell）