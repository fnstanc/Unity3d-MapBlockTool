# MapBlockTool
---
Unity3d地图占位工具

地图网格按照Unity世界坐标分割，网格默认长度为1。

## 使用方法
---
 - 确保地图有Unity地形
 - Tool -> MapBlock Tool生成网格可视化对象
 - 在Hierachy选中生成的MapBlockVisualizer对象

![screenshot_0](https://raw.githubusercontent.com/fnstanc/Unity3d-MapBlockTool/master/ScreenShots/0.png)

 - 在Inspector创建地图网格，并激活brushMode
 - 在场景中按鼠标左键刷占位，按Ctrl+鼠标左键擦出占位

![screenshot_1](https://raw.githubusercontent.com/fnstanc/Unity3d-MapBlockTool/master/ScreenShots/1.png)

 - 占位数据在保存在MapBlock中，通过MapBlock序列化和反序列化到文件

## 在项目中使用
---
把Scripts下文件拷贝到项目中，修改网格资源路径MapBlockVisualizerInspector.GridResPath为新的目录

## 致谢
---
本项目受[unity-terrainGrid](https://bitbucket.org/xuanyusong/unity-terraingrid)启发，感谢[雨松MOMO](http://www.xuanyusong.com/)。
