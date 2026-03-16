目前有PinBallBase.cs，这是弹球的所有逻辑类，base是表示这是基类，将来会有各种继承它的特殊弹球(PinBall)。

PinBallBase需要再有一个对应的Render类，用于渲染弹球的外观(暂用Image)。

PinBall就是一个圆形弹球，由Player发射出来，初始方向也源于Player。速度是自身属性，碰到上左右三面Border时，会根据

当前方向和Border的方向，发生镜面反射。反射后的速度量由自身的速度变化属性决定，可以变快也可以变慢。同时也应该有

最小速度的限制，防止PinBall静止不动。

PinBall应该由缓存池管理，数量会比较多，创建和隐藏对应着进出缓存池。
