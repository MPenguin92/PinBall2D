游戏单位Unit，一个正方形的游戏单位。应该建立一个基类，如UnitBase.cs,以备将来继承实现特殊的unit。

需要有一个Render类，渲染形象（暂定Image）。

unit有生命值的属性，当PinBall接触到Unit，会扣除生命值，当生命值为0则隐藏，回归缓存池。并根据当前位置，做镜面反射，和Border反射逻辑类似。
