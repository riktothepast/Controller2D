# Controller2D

A simple Character Controller for 2D games that require tight movement. It requieres a Collider2D.

##### Collider type #####
By default the collision shape to cast against the environment will be a square based on the bounds of the `Collider` attached to the game object. I you set a `Capsule Collider 2D` the controller will use a `Capsule` to check for collisions.

##### Solid Mask #####
Defines the layer masks on which the controller should check for collisions.

##### Slopes #####
If the `Manage Slopes` toggle is active the Controller will be able to ascend/descend slopes, to set the max slope angle use the `Max Slope Angle` slider.

### Methods

```C#
  public void Move(Vector3 deltaStep)
````
Will try to move the controller in the given direction, will resolve collisions and will align accordingly.

```C#
  public CollisionState CollisionState()
```
A Struct that provides collision information from last frame, will provide collision data for `above`, `below`, `left` and `right` collisions.


### Trigger Detection

You can bind delegate events to detect triggers, in order to detect a trigger the controller or the other GO should have a `Collider2D` in Trigger mode and a `RigidBody2D` set as `Kitematic` or `Static` to avoid Physics issues.

You can bind to:

- OnTriggerEnter
- OnTriggerStay
- OnTriggerExit


```C#
    private void Awake()
    {
        controller2D = GetComponent<Controller2D>();
        controller2D.onTriggerEnter += OnTriggerEnter2D;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Destroy(collision.gameObject);
    }
````


![Controller2D](https://github.com/riktothepast/Controller2D/blob/master/ccMovement.gif)

