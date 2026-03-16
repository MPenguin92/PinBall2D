目前有Player.cs是所以逻辑在这里，PlayerRender.cs是所有渲染相关的逻辑。

功能需求：

player就是玩家的主控单位，不能移动，但是可以通过a、d操作左右旋转。

正y方向为正方向，左右旋转均不能超过80度。

点击f可以从player位置，沿当前方向发射PinBall。player拥有的Pinball有上限，也有发射的间隔限制。

当pinball发射完毕之后，就不能继续发射，除非重新补充到新的Pinball。


playerRender除了要渲染player的形象（暂时用Image），还应该渲染方向，使用LineRender的方式渲染一条直线，

长度可以参数控制。碰到unit暂停继续向前渲染，碰到border，则根据入射角度，做镜面反射继续向新的方向渲染，

直到达到最大长度。
