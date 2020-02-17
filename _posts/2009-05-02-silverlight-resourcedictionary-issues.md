---
layout: article
title: Silverlight ResourceDictionary Issues
categories: [development]
tags: [bugs, silverlight, tips]
---
I have been developing WPF/Silverlight Applications about half an year already and I am charmed by the power of those technologies. But still there are some very strange and annoying issues I came upon, especially in my Silverlight implementations. The most favourite one is the one I will be explaining in this post. I am talking about the ResourceDictionary class.

### Little intro

ResourceDictionary is a major class in the Silverlight platform, used to hold all the needed resources that you might create and use during your application lifecycle. This includes Custom Templates, Styles, Brushes, Images and many other very cool and useful stuff. This class is used by the runtime in order to choose and apply all these nice peaces you have put in. This brings to mind that this is one of the fundamental classes of the framework and therefore it is very well structured.

### The dissapointment

This is where it gets messy. Having in mind that this is a `Resource**Dictionary**` you might think that it has all the great features that the standart Dictionary class reveals. Since it is being derived from `IEnumerable` interface, you might think that you can easily `foreach` that Dictionary and circle around your resources. Well you thought wrong ! This is how the `GetEnumerator` method used by the `foreach` looks like:

```c#
IEnumerable GetEnumerator()
{
    throw new NotImplementedException();
}
```

So if you try to run this very simple code:

```c#
ControlTemplate tmp;
ResourceDictionary dict = new ResourceDictionary();
dict.Add("template1", new ControlTemplate());
dict.Add("template2", new Style());
foreach (object obj in dict)
{
    if (obj is ControlTemplate)
    tmp = obj as ControlTemplate;
}
```

you will get this nice thingy here:
![NotImplementedException](/assets/img/notimplemented.png){:.shadow}

The sad thing is that this was a behaviour in Silverlight 2 Final release and it is still present in Silverlight 3 Beta. I really hope this will be exposed in the Final release of Silverlight, because personally in my opinion this is quite useful in some complicated scenarios.

### MergedDictionaries

This was a problem not being solved in Silverlight 2, but in the 3rd version it has been resolved, so I am not going to brag about it. Still I have been experiencing some difficulties in using them in the `generic.xaml`, but I am planning to write a separate article on that topic.

### The conclusion

I want to say that this article of mine is not intended to mess around with the Silverlight guys from Microsoft. Silverlight is a great technology and I absolutely love it. I have been developing Silverlight applications for almost an year now and I tend to use it in every project I am involved in. I am pointing out those issues just to help the other Silverlight developers, who stumble upon those problems and can't figure out why. I have spent days trying to fix some of the issues I am writing about and most of the time I spent on figuring out why it is happening. The more developers I can help by sharing my experience the better !
