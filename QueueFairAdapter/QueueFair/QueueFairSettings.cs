//-----------------------------------------------------------------------
// <copyright file="QueueFairSettings.cs" company="Matt King for Orderly Telecoms">
// Copyright Matt King. All rights Reserved
// </copyright>
//-----------------------------------------------------------------------
namespace QueueFair.Adapter
{
    using System.Collections.Generic;

    public class QueueFairSettings
    {
        public QueueFairSettings(dynamic json)
        {
                this.Queues = new Queue[json.queues.Count];
                for (int i = 0; i < json.queues.Count; i++)
                {
                    this.Queues[i] = new Queue(json.queues[i]);
                    if (this.QueuesByName.ContainsKey(this.Queues[i].Name))
                    {
                        continue;
                    }

                    this.QueuesByName.Add(this.Queues[i].Name, this.Queues[i]);
                }
        }

        public Queue[] Queues { get; set; } = new Queue[0];

        public Dictionary<string, Queue> QueuesByName { get; set; } = new Dictionary<string, Queue>();

        public Queue GetQueueByName(string name)
        {
            Queue ret = null;
            this.QueuesByName.TryGetValue(name, out ret);
            return ret;
        }

        public class Queue
        {
            public Queue(dynamic json)
            {
                this.Name = json.name;
                this.DisplayName = json.displayName;
                this.AdapterMode = json.adapterMode;
                this.AdapterServer = json.adapterServer;
                this.QueueServer = json.queueServer;
                this.CookieDomain = json.cookieDomain;
                this.DynamicTarget = json.dynamicTarget;
                this.PassedLifetimeMinutes = json.passedLifetimeMinutes;
                this.Secret = json.secret;

                if (json.activation == null)
                {
                    return;
                }

                if (json.activation.rules != null)
                {
                    this.Rules = new Rule[json.activation.rules.Count];
                    for (int i = 0; i < json.activation.rules.Count; i++)
                    {
                        this.Rules[i] = new Rule(json.activation.rules[i]);
                    }
                }

                if (json.activation.variantRules != null)
                {
                    this.VariantRules = new Variant[json.activation.variantRules.Count];
                    for (int i = 0; i < json.activation.variantRules.Count; i++)
                    {
                        this.VariantRules[i] = new Variant(json.activation.variantRules[i]);
                    }
                }
            }

            public string Name { get; set; } = null;

            public string DisplayName { get; set; } = null;

            public string AdapterMode { get; set; } = null;

            public string AdapterServer { get; set; } = null;

            public string QueueServer { get; set; } = null;

            public string CookieDomain { get; set; } = null;

            public string DynamicTarget { get; set; } = null;

            public string Secret { get; set; } = null;

            public int PassedLifetimeMinutes { get; set; } = 60;

            public Rule[] Rules { get; set; } = new Rule[0];

            public Variant[] VariantRules { get; set; } = new Variant[0];
        }

        public class Variant
        {
            public Variant(dynamic json)
            {
                this.VariantName = json.variant;
                if (json.rules != null)
                {
                    this.Rules = new Rule[json.rules.Count];
                    for (int i = 0; i < json.rules.Count; i++)
                    {
                        this.Rules[i] = new Rule(json.rules[i]);
                    }
                }
            }

            public string VariantName { get; set; } = null;

            public Rule[] Rules { get; set; } = new Rule[0];
        }

        public class Rule
        {
            public Rule(dynamic json)
            {
                this.Operator = json.@operator;
                this.Component = json.component;
                this.Name = json.name;
                this.Value = json.value;
                this.CaseSensitive = json.caseSensitive;
                this.Negate = json.negate;
                this.Match = json.match;
            }

            public string Operator { get; set; } = null;

            public string Component { get; set; } = null;

            public string Name { get; set; } = null;

            public string Value { get; set; } = null;

            public string Match { get; set; } = null;

            public bool CaseSensitive { get; set; } = false;

            public bool Negate { get; set; } = false;
        }
    }
}