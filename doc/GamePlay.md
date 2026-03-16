游戏逻辑：

现有GameLogicManager.cs的单例，用于控制整个游戏的逻辑，以及一些辅助功能。

GamePlay负责Update整个游戏，包括PinBall、Player、Unit都不应该有独立的Update，

而是由GamePlay统一调用Tick方法，隐藏(缓存池内)的物体不需要执行逻辑。


GamePlay里同时应该管理着PinBall和unit的缓存池，缓存技术可以用unity自带的缓存池功能。加入池则隐藏显示并挪到独立

的缓存根节点下，退出池则是离开独立的缓存节点，并显示出来正常执行对应的业务逻辑。
