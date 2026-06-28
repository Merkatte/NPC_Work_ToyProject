using System;
using System.Collections.Generic;
using UnityEngine;

namespace Recruitment
{
    [Serializable]
    public class CandidateStatLine
    {
        [SerializeField] private string _label;
        [SerializeField] private float _value;

        public string Label => _label;
        public float Value => _value;
    }

    // Display-only stat summary shown on a candidate card.
    // Label/value pairs are designer-defined; no balance rules are baked in.
    [Serializable]
    public class CandidateStatPreview
    {
        [SerializeField] private CandidateStatLine[] _lines;

        public IReadOnlyList<CandidateStatLine> Lines =>
            _lines ?? Array.Empty<CandidateStatLine>();
    }
}
