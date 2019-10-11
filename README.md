# Controller2D

A simple Character Controller for 2D games that require tight movement. It requieres a BoxCollider2D.

### Methods

```C#
public void Move(Vector3 deltaStep)
````
Will try to move the controller in the given direction, will resolve collisions and will align accordingly.

```C#
public CollisionState CollisionState()
```
A Struct that provides collision information from last frame, will provide collision data for `above`, `below`, `left` and `right` collisions.

![Controller2D](https://github.com/riktothepast/Controller2D/blob/master/ccMovement.gif)
