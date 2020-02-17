---
layout: article
title: "[Best Practice #1] Override the ToString() method of your classes"
categories: [development]
tags: [best practices, c#]
---

This is first of series of best practices and tips I have for my fellow software developers. The articles are not ordered by importance, they are completely random. (And I mean real random, not computer random)
There are a few methods available for you from the [object](https://msdn.microsoft.com/en-us/library/system.object(v=vs.110).aspx) class in .NET, which provide some basic functionalities for you structures. There's a bunch of them, but giving definition and explanation for each one is not a target of our article. The one I am going to point out here is

```c#
public virtual string ToString()
```

**Why this is important?**

Well it is definitely not "a must", but I have found it to be dramatically helpful during debugging. Let me show you why.

Imagine this simple class:

```c#
public class Person
{
    public Person(int id, string firstName, string lastName)
    {
        Id = id;
        FirstName = firstName;
        LastName = lastName;
    }
    public int Id { get; }
    public string FirstName { get; }
    public string LastName { get; }
}
```

Now if I am doing some application, which populates a list of type `List<Person>`, by loading it from a database and I need to debug it, this is how my Quick Watch would look like:
![Quick Watch](/assets/img/QuickWatch1.png)

As you can see, if I want to see the details of each object, in order to determine if some Person is there, I need to expand them one by one. Here I have only 5, but having a collection of thousands of items is not something unusual.
The trick that comes in handy is the previously mentioned `ToString()` method of the object base class. That method is there for a good reason. Whenever the .NET framework needs to convert some object into a textual representation, this is when that method is invoked. Also this method is used by tools like Visual Studio, to populate simple textbox based controls as the Value column we see in the screenshot.

Now let's override our method in the Person class and then take a look at the QuickWatch. Here is our implementation:

```c#
public override string ToString()
{
    return $"[{Id}] {FirstName} {LastName}";
}
```

As you can see, we are just returning somewhat meaningful string in that method. We are doing this by using [string interpolation](https://msdn.microsoft.com/en-us/library/dn961160.aspx). By doing this, here is our new screenshot:
![Quick Watch](/assets/img/QuickWatch2.png)

Here you can see, that our Value column looks much better now. I can see the text interpretation of our class here and not having to expand the objects in order to know, which object is the one with Id = 3.
Another place, where `ToString()` is used is in the string interpolation. Whenever you put an instance of an object in the curly brackets, the .NET is using the `ToString()` method in order to fill that. So if we have the following code:

```c#
Console.WriteLine($"The first person in the list is: {list[0]}");
```

This is the difference we have:
![Console](/assets/img/console1.png)

You see, if you don't have a `ToString()` method overridden, this means that the default one will be used, that is implemented in the object class. And that one returns a string with the full class name.
With everything said, I want to show you how helpful that practice can be in your daily developer's life. Debugging and writing things in the console is usually big part of what every developer does on a daily basis. Simply overriding the `ToString()` method can save you a lot of time and energy, and usually it is one line of code.
