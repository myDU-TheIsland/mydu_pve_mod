using System;
using System.Numerics;
using Mod.DynamicEncounters.Helpers;
using NQ;

namespace Mod.DynamicEncounters.Vector.Helpers;

public static class VelocityHelper
{
    public static double CalculateBrakingDistance(double velocity, double deceleration)
    {
        return Math.Pow(velocity, 2) / (2 * deceleration);
    }

    public static bool ShouldStartBraking(Vector3 currentPosition, Vector3 targetPosition, Vector3 currentVelocity,
        double deceleration)
    {
        double remainingDistance = Vector3.Distance(currentPosition, targetPosition);

        var brakingDistance = CalculateBrakingDistance(currentVelocity.Length(), deceleration);

        return remainingDistance <= brakingDistance;
    }

    public static Vec3 LinearInterpolateWithAcceleration(
        Vec3 start,
        Vec3 end,
        ref Vec3 velocity,
        Vec3 acceleration,
        double clampSize,
        double deltaTime,
        bool handleOvershoot = false
    )
    {
        // Calculate direction and distance to the end
        var direction = new Vec3
        {
            x = end.x - start.x,
            y = end.y - start.y,
            z = end.z - start.z
        };

        var distance = direction.Size();

        // Check if distance is very small (to avoid division by zero)
        if (distance < 0.001)
        {
            return end;
        }

        // Update velocity based on acceleration and apply half of acceleration for position calculation
        Vec3 displacement = new Vec3
        {
            x = velocity.x * deltaTime + 0.5 * acceleration.x * deltaTime * deltaTime,
            y = velocity.y * deltaTime + 0.5 * acceleration.y * deltaTime * deltaTime,
            z = velocity.z * deltaTime + 0.5 * acceleration.z * deltaTime * deltaTime
        };

        // Update velocity after position calculation (Euler integration)
        velocity = new Vec3
        {
            x = velocity.x + acceleration.x * deltaTime,
            y = velocity.y + acceleration.y * deltaTime,
            z = velocity.z + acceleration.z * deltaTime
        };

        velocity = velocity.ClampToSize(clampSize);

        // Calculate the new position based on the displacement
        var newPosition = new Vec3
        {
            x = start.x + displacement.x,
            y = start.y + displacement.y,
            z = start.z + displacement.z
        };

        // Check for NaN values and handle them
        if (double.IsNaN(newPosition.x) || double.IsNaN(newPosition.y) || double.IsNaN(newPosition.z) ||
            double.IsNaN(velocity.x) || double.IsNaN(velocity.y) || double.IsNaN(velocity.z))
        {
            // Handle NaN case by setting position to end and stopping velocity
            newPosition = new Vec3 { x = end.x, y = end.y, z = end.z };
        }

        if (handleOvershoot)
        {
            // Ensure we do not overshoot the end position
            if ((newPosition - start).Size() > distance)
            {
                newPosition = end;
                velocity = new Vec3 { x = 0, y = 0, z = 0 }; // Stop the velocity at the end
            }
        }

        return newPosition;
    }

    public static Vec3 LinearInterpolateWithVelocity(
        Vec3 start,
        Vec3 end,
        ref Vec3 velocity,
        Vec3 acceleration,
        double clampSize,
        double deltaTime)
    {
        // Calculate direction and distance to the end
        var direction = new Vec3
        {
            x = end.x - start.x,
            y = end.y - start.y,
            z = end.z - start.z
        };

        var distance = direction.Size();

        // Check if distance is very small (to avoid division by zero)
        if (distance < 0.001)
        {
            return end;
        }

        // Update velocity based on acceleration
        velocity = new Vec3
        {
            x = velocity.x + acceleration.x * deltaTime,
            y = velocity.y + acceleration.y * deltaTime,
            z = velocity.z + acceleration.z * deltaTime
        };

        velocity = velocity.ClampToSize(clampSize);

        // Calculate the new position based on the updated velocity
        var newPosition = new Vec3
        {
            x = start.x + velocity.x * deltaTime,
            y = start.y + velocity.y * deltaTime,
            z = start.z + velocity.z * deltaTime
        };

        // Calculate the new distance to the end
        var newDirection = new Vec3
        {
            x = end.x - newPosition.x,
            y = end.y - newPosition.y,
            z = end.z - newPosition.z
        };

        var newDistance = newDirection.Size();

        // Check if the object is close to the target and the distance change is smaller than acceleration
        if (newDistance < 0.001 || newDistance < acceleration.Size() * deltaTime)
        {
            // Close to the destination; set position to the end and stop velocity
            newPosition = new Vec3 { x = end.x, y = end.y, z = end.z };
            // velocity = new Vec3 { x = 0, y = 0, z = 0 };
        }

        // Check for NaN values and handle them
        if (double.IsNaN(newPosition.x) || double.IsNaN(newPosition.y) || double.IsNaN(newPosition.z) ||
            double.IsNaN(velocity.x) || double.IsNaN(velocity.y) || double.IsNaN(velocity.z))
        {
            // Handle NaN case by setting position to end and stopping velocity
            newPosition = new Vec3 { x = end.x, y = end.y, z = end.z };
            // velocity = new Vec3 { x = 0, y = 0, z = 0 };
        }

        return newPosition;
    }

    public static Vec3 ApplyBraking(
        Vec3 start,
        ref Vec3 velocity,
        double decelerationRate,
        double deltaTime
    )
    {
        // Check if velocity is already near zero
        if (velocity.Size() < 0.001)
        {
            velocity = new Vec3 { x = 0, y = 0, z = 0 }; // Stop completely if very small
            return start;
        }

        // Calculate deceleration magnitude for this frame
        var decelerationMagnitude = decelerationRate * deltaTime;

        // Apply braking force, ensuring we don’t overshoot past zero
        if (Math.Abs(velocity.x) <= decelerationMagnitude)
            velocity.x = 0;
        else
            velocity.x += velocity.x > 0 ? -decelerationMagnitude : decelerationMagnitude;

        if (Math.Abs(velocity.y) <= decelerationMagnitude)
            velocity.y = 0;
        else
            velocity.y += velocity.y > 0 ? -decelerationMagnitude : decelerationMagnitude;

        if (Math.Abs(velocity.z) <= decelerationMagnitude)
            velocity.z = 0;
        else
            velocity.z += velocity.z > 0 ? -decelerationMagnitude : decelerationMagnitude;

        // Calculate displacement for this frame based on updated velocity
        Vec3 displacement = new Vec3
        {
            x = velocity.x * deltaTime,
            y = velocity.y * deltaTime,
            z = velocity.z * deltaTime
        };

        // Calculate the new position based on the displacement
        var newPosition = new Vec3
        {
            x = start.x + displacement.x,
            y = start.y + displacement.y,
            z = start.z + displacement.z
        };

        return newPosition;
    }

    public static Vec3 CalculateFuturePosition(
        Vec3 currentPosition,
        Vec3 velocity,
        double deltaTime,
        double futureSeconds
    )
    {
        // Number of steps to simulate
        var steps = (int)(futureSeconds / deltaTime);

        // Calculate future position iteratively
        var futurePosition = currentPosition;

        for (var i = 0; i < steps; i++)
        {
            futurePosition = new Vec3
            {
                x = futurePosition.x + velocity.x * deltaTime,
                y = futurePosition.y + velocity.y * deltaTime,
                z = futurePosition.z + velocity.z * deltaTime
            };
        }

        return futurePosition;
    }
}