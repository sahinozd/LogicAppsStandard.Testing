Feature: Nested-Foreach-and-Until-Loops-Sample-StepDefinition
	As a integration specialist
	I want to verify that to test the step definition functionality in the test framework

# Validate complex nested loop and condition scenarios
Scenario: Validate complete workflow with nested structures

    When Workflow "prc-nestedloops-and-do-until" is triggered
    
    # ===== Top level ACTIONS =====
    Then The workflow executed these actions:
    | StepName                                      | Status    |
    | Recurrence                                    | Succeeded |
    | Initialize variables                          | Succeeded |
    | Set variable                                  | Succeeded |

    # ===== or =====
    And The "Set variable" action has status "Succeeded"

    # ===== Try SCOPE =====
    And In "Try":
    | StepName                                      | Status    |
    | For each number                               | Succeeded |
    | Until                                         | Succeeded |

    # ===== A single FOREACH loop=====
    And In "Try.For each number[1]":
    | StepName                                      | Status    |
    | For each letter                               | Succeeded |
    | Variable testString2 value less than 3        | Succeeded |
    
    # ===== or (top level iteration) =====
    And All iterations of "For each number" executed:
    | StepName                                      | Status    |
    | For each letter                               | Succeeded |
    | Variable testString2 value less than 3        | Succeeded |

    # ===== Triple nested FOREACH loop =====
    And In "Try.For each number[1].For each letter[2].For each character[3]":
    | StepName                                      | Status    |
    | Set variable testString                       | Succeeded |

    # ===== or (deepest level iteration) =====
    And All iterations of "For each character" executed:
    | StepName                                      | Status    |
    | Set variable testString                       | Succeeded |

     # ===== A single UNTIL loop=====
    And In "Try.Until[1]":
    | StepName                                      | Status    |
    | Until2                                        | Succeeded |
    | Reset variable counter2                       | Succeeded |
    | Set variable teststring until                 | Succeeded |
    | Increment variable counter                    | Succeeded |
    | Condition                                     | Succeeded |
    And In "Try.Until[1].Condition":
    | StepName                                      | Status    |
    | Set variable untilCompleted                   | Skipped   |

    # ===== Triple nested UNTIL loop =====
    And In "Try.Until[1].Until2[2].Until3[2]":
    | StepName                                      | Status    |
    | Increment variable counter3                   | Succeeded |

    # ===== or (top level iteration) =====
    And All iterations of "Until" executed:
    | StepName                                      | Status    |
    | Until2                                        | Succeeded |
    | Reset variable counter2                       | Succeeded |
    | Set variable teststring until                 | Succeeded |
    | Increment variable counter                    | Succeeded |
    | Condition                                     | Succeeded |

    # ===== or (deepest level iteration) =====
    And All iterations of "Until3" executed:
    | StepName                                      | Status    |
    | Increment variable counter3                   | Succeeded |

    # ===== CONDITION in Try scope =====
    And In "Try":
    | StepName                                      | Status    |
    | Condition in try                              | Succeeded |

    # ===== CONDITION IF ACTION CHECK in Try scope =====
    And In the "actions" branch of "Condition in try":
    | StepName                                      | Status    |
    | Set variable testString in condition          | Succeeded |

    # ===== or.... =====
    And In "Try.Condition in try.actions":
    | StepName                                      | Status    |
    | Set variable testString in condition          | Succeeded |

    # ===== CONDITION in a foreach =====
    And In "Try.For each number[1].Variable testString2 value less than 3.actions":
    | StepName                                      | Status    |
    | Set variable testString2 true                 | Succeeded |


    # ===== SWITCH in Try scope =====
    And In "Try":
    | StepName                                      | Status    |
    | Switch                                        | Succeeded |

    # ===== SWITCH IF ACTION CHECK in Try scope =====
    And In the "Default" branch of "Switch":
    | StepName                                      | Status    |
    | Set variable in switch                        | Succeeded |

    # ===== or.... =====
    And In "Try.Switch.Default":
    | StepName                                      | Status    |
    | Set variable in switch                        | Succeeded |

    # ===== SWITCH in a foreach =====
    And In "Try.For each number[1].Variable testString2 value less than 3.actions":
    | StepName                                      | Status    |
    | Set variable testString2 true                 | Succeeded |
    And In "Try.For each number[1].Variable testString2 value less than 3.else":
    | StepName                                      | Status    |
    | Set variable testString2 false                | Skipped   |
    
 