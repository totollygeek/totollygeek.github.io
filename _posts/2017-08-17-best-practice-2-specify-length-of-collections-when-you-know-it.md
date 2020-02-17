---
layout: article
title: "[Best Practice #2] Specify length of collections when you know it."
categories: [development]
tags: [best practices, c#, dev]
---

In the past of programming, developers were fighting for every single byte of memory and every single computing cycle. But with advancement of computers, with memory getting more and more cheaper, with programming languages getting more user friendly and offering compiler sugar, developers got lazier. But this is a good thing. Software gets insanely complex, which requires powerful computers and high level programming languages, that provide many things out-of-the-box.

Unfortunately times change. Today we live in the cloud. We need to design our software so it can withstand massive loads of traffic and parallel execution. This paradigm kinda brings back the old days. It starts to really matter what we do under the hood. Each line is again important. Each object reference is crucial.

In that direction, today I want to talk about the useful collection classes like [`List<T>`](https://msdn.microsoft.com/en-us/library/6sh2ey19(v=vs.110).aspx) and [`Queue<T>`](https://msdn.microsoft.com/en-us/library/7977ey2c(v=vs.110).aspx) that the .NET Framework provides for us. Out of the box many times developers overuse those classes without thinking of the consequences. One particular feature I want to stress in this article is the two different constructors that those classes have. I will focus on `List<T>`.

The class has three constructors, but I will talk about two of them.

- The standard parameterless one: [`List<T>()`](https://msdn.microsoft.com/en-us/library/4kf43ys3(v=vs.110).aspx)
- And the one that accepts a capacity: [`List<T>(Int32 capacity)`](https://msdn.microsoft.com/en-us/library/dw8e0z9z(v=vs.110).aspx)

Do you remember the time before `List<T>` was available? How do we represent collections of the same item in programming? We use arrays, of course. And there comes this magical class that gives us a collection that you can easily Add and Remove items to. Because as you know, arrays, once declared, have a fixed number of elements. If you wanted to add elements to the collection, you needed to create a new array with bigger capacity and copy all of the old elements to the new array.

Well, as it comes to no surprise, this is exactly what `List<T>` is doing. A quick look at the class [source code](http://referencesource.microsoft.com/#mscorlib/system/collections/generic/list.cs,cf7f4095e4de7646), we can see that there is a private member:

```c#
private T[] _items;
```

So what happens when we create a `List<T>` with no capacity? An empty array is being created and from that point, every time we add an element, the code sets new capacity to the array by creating a bigger one and coping the elements to the new one.

On the other hand if we set the capacity beforehand in the constructor, then all of the adding of elements just populates elements of the already allocated array.

You can imagine how this can impact performance and memory consumption.

To demonstrate the difference I have created a small test using the awesome [BenchmarkDotNet](http://benchmarkdotnet.org) library.

```c#
using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Running;

namespace Blog.BestPractices
{
    class Program
    {
        static void Main(string[] args)
        {
            var summary = BenchmarkRunner.Run<TestLists>();
            Console.ReadLine();
        }
    }
    [MemoryDiagnoser]
    public class TestLists
    {
        private const int _iterations = 1000000;
        [Benchmark]
        public void TestNormalList()
        {
            var list = new List<byte>();
            for (var i = 0; i < _iterations; i++)
            {
                list.Add(123);
            }
        }
        [Benchmark]
        public void TestScopedList()
        {
            var listWithLength = new List<byte>(_iterations);
            for (var i = 0; i < _iterations; i++)
            {
                listWithLength.Add(123);
            }
        }
    }
}
```

And here are the results of that benchmark:

```text
// * Summary *
BenchmarkDotNet=v0.10.9, OS=Mac OS X 10.12
Processor=Intel Core i5-4250U CPU 1.30GHz (Haswell), ProcessorCount=4
.NET Core SDK=1.0.4
  [Host]     : .NET Core 1.1.2 (Framework 4.6.25211.01), 64bit RyuJIT
  DefaultJob : .NET Core 1.1.2 (Framework 4.6.25211.01), 64bit RyuJIT
         | Method         |     Mean |     Error |    StdDev |    Gen 0 |    Gen 1 |    Gen 2 |  Allocated |
         | -------------- | -------: | --------: | --------: | -------: | -------: | -------: | ---------: |
         | TestNormalList | 6.349 ms | 0.0941 ms | 0.0880 ms | 492.1875 | 492.1875 | 492.1875 | 2048.48 KB |
         | TestScopedList | 5.279 ms | 0.0636 ms | 0.0595 ms | 242.1875 | 242.1875 | 242.1875 |  976.63 KB |
```

You can see that using the list with fixed capacity allocated two times less memory and has better performance.

In conclusion I want to say, that I am perfectly aware that this example does not fit everywhere, because sometimes you just don't know how much elements you would need. And the is totally fine. What I am saying is that you should always keep in mind the numbers above and when you instanciate a list, ask yourself if you cannot determine its size.
