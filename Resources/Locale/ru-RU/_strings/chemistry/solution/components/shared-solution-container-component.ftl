shared-solution-container-component-on-examine-empty-container = Не содержит вещества.
shared-solution-container-component-on-examine-main-text = Содержит [color={ $color }]{ $desc }[/color] { $chemCount }
shared-solution-container-component-on-examine-worded-amount-one-reagent = вещество.
shared-solution-container-component-on-examine-worded-amount-multiple-reagents = смесь веществ.
examinable-solution-has-recognizable-chemicals = В этом растворе вы можете распознать { $recognizedString }.
examinable-solution-recognized-first = [color={ $color }]{ $chemical }[/color]
examinable-solution-recognized-next = , [color={ $color }]{ $chemical }[/color]
examinable-solution-recognized-last = и [color={ $color }]{ $chemical }[/color]
examinable-solution-recognized = [color={ $color }]{ $chemical }[/color]
examinable-solution-on-examine-volume-puddle =
    The puddle is { $fillLevel ->
        [exact] [color=white]{ $current }u[/color].
        [full] huge and overflowing!
        [mostlyfull] huge and overflowing!
        [halffull] deep and flowing.
        [halfempty] very deep.
       *[mostlyempty] pooling together.
        [empty] forming multiple small pools.
    }
