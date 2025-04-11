using System;
using System.Drawing;
using Microsoft.Xna.Framework;
using Point = System.Drawing.Point;

namespace UIInfoSuite2.Infrastructure.Models.Layout.Measurement;

public struct Dimensions(int width, int height) : IEquatable<Dimensions>
{
  public int Width = width;
  public int Height = height;

  public Dimensions() : this(0, 0) { }
  public Dimensions(float width, float height) : this((int)width, (int)height) { }

  public void Deconstruct(out int width, out int height)
  {
    width = Width;
    height = Height;
  }

  public static Dimensions Empty => new();

  // Arithmetic operators
  public static Dimensions operator +(Dimensions left, Dimensions right)
  {
    return new Dimensions(left.Width + right.Width, left.Height + right.Height);
  }

  public static Dimensions operator -(Dimensions left, Dimensions right)
  {
    return new Dimensions(left.Width - right.Width, left.Height - right.Height);
  }

  public static Dimensions operator *(Dimensions dimensions, double scalar)
  {
    return new Dimensions((int)(dimensions.Width * scalar), (int)(dimensions.Height * scalar));
  }

  public static Dimensions operator *(double scalar, Dimensions dimensions)
  {
    return dimensions * scalar;
  }

  public static Dimensions operator /(Dimensions dimensions, double scalar)
  {
    return new Dimensions((int)(dimensions.Width / scalar), (int)(dimensions.Height / scalar));
  }

  public static Dimensions operator %(Dimensions dimensions, double scalar)
  {
    return new Dimensions((int)(dimensions.Width % scalar), (int)(dimensions.Height % scalar));
  }

  public static Dimensions operator -(Dimensions dimensions)
  {
    return new Dimensions(-dimensions.Width, -dimensions.Height);
  }

  // Equality operators
  public static bool operator ==(Dimensions left, Dimensions right)
  {
    return left.Equals(right);
  }

  public static bool operator !=(Dimensions left, Dimensions right)
  {
    return !left.Equals(right);
  }

  // Conversion methods
  public Vector2 ToVector2()
  {
    return new Vector2(Width, Height);
  }

  public static Dimensions FromVector2(Vector2 vector)
  {
    return new Dimensions((int)vector.X, (int)vector.Y);
  }

  public Point ToPoint()
  {
    return new Point(Width, Height);
  }

  public static Dimensions FromPoint(Point point)
  {
    return new Dimensions(point.X, point.Y);
  }

  public Size ToSize()
  {
    return new Size(Width, Height);
  }

  public static Dimensions FromSize(Size size)
  {
    return new Dimensions(size.Width, size.Height);
  }

  // Additional utility methods
  public readonly Dimensions Abs()
  {
    return new Dimensions(Math.Abs(Width), Math.Abs(Height));
  }

  public readonly int Area => Width * Height;

  public readonly double Diagonal => Math.Sqrt(Width * Width + Height * Height);

  public void Scale(double factor)
  {
    Width = (int)(Width * factor);
    Height = (int)(Height * factor);
  }

  public readonly Dimensions Scaled(double factor)
  {
    return new Dimensions((int)(Width * factor), (int)(Height * factor));
  }

  // Object overrides
  public readonly override bool Equals(object? obj)
  {
    return obj is Dimensions other && Equals(other);
  }

  public readonly bool Equals(Dimensions other)
  {
    return Width == other.Width && Height == other.Height;
  }

  // Note: Since the struct is mutable, GetHashCode() should be marked with the readonly modifier
  // to ensure it uses the current state of the fields
  public readonly override int GetHashCode()
  {
    return HashCode.Combine(Width, Height);
  }

  public readonly override string ToString()
  {
    return $"Width: {Width}, Height: {Height}";
  }

  // Implicit conversions
  public static implicit operator Size(Dimensions dimensions)
  {
    return dimensions.ToSize();
  }

  public static implicit operator Point(Dimensions dimensions)
  {
    return dimensions.ToPoint();
  }

  public static implicit operator Vector2(Dimensions dimensions)
  {
    return dimensions.ToVector2();
  }

  // Explicit conversions
  public static explicit operator Dimensions(Size size)
  {
    return FromSize(size);
  }

  public static explicit operator Dimensions(Point point)
  {
    return FromPoint(point);
  }

  public static explicit operator Dimensions(Vector2 vector)
  {
    return FromVector2(vector);
  }
}
