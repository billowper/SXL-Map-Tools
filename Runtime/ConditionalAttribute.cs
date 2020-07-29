using UnityEngine;

public abstract class ConditionalAttribute : PropertyAttribute
{
    public string ConditionalPropertyName { get; }
    public object TestValue { get; }

    protected ConditionalAttribute(string conditionalPropertyName)
    {
        ConditionalPropertyName = conditionalPropertyName;
    }

    protected ConditionalAttribute(string conditionalPropertyName, object value) : this (conditionalPropertyName)
    {
        TestValue = (int) value;
    }
}

public class ShowIfAttribute : ConditionalAttribute
{
    public ShowIfAttribute(string conditionalPropertyName) : base(conditionalPropertyName)
    {
    }

    public ShowIfAttribute(string conditionalPropertyName, object value) : base(conditionalPropertyName, value)
    {
    }
}

public class HideIfAttribute : ConditionalAttribute
{
    public HideIfAttribute(string conditionalPropertyName) : base(conditionalPropertyName)
    {
    }

    public HideIfAttribute(string conditionalPropertyName, object value) : base(conditionalPropertyName, value)
    {
    }
}