Feature: Sprint Management
    As a Manager or Admin user
    I want to manage sprints within projects
    So that I can organize work into time-boxed iterations

Background:
Given the application is running
And I am logged in as "venkateshboyapati96@gmail.com" with password "Govinda@1117"
And a project "Test Project" with key "TEST" exists

@sprints @smoke
Scenario: Create a new sprint successfully
    Given I am on the sprints page
    When I create a new sprint with the following details:
        | Field       | Value                              |
        | Project     | Test Project                       |
        | Name        | Sprint 1                           |
        | Goal        | Complete authentication features   |
        | Start Date  | 2024-03-01                         |
        | End Date    | 2024-03-14                         |
    Then the sprint should be created successfully
    And I should see the sprint "Sprint 1" in the sprints list
    And the sprint status should be "Planning"

@sprints
Scenario: View sprints list
    Given I am on the sprints page
    Then I should see the sprints page
    And I should see a list of sprints or empty state

@sprints
Scenario: Create multiple sprints for same project
    Given I am on the sprints page
    When I create sprint "Sprint 1" for project "Test Project"
    And I create sprint "Sprint 2" for project "Test Project"
    Then I should see 2 sprints in the list
    And sprint "Sprint 1" should have number 1
    And sprint "Sprint 2" should have number 2

@sprints @workflow
Scenario: Sprint lifecycle - Planning to Active
    Given I am on the sprints page
    And a sprint "Sprint 1" in "Planning" status exists
    When I start the sprint "Sprint 1"
    Then the sprint status should change to "Active"

@sprints @workflow
Scenario: Sprint lifecycle - Active to Completed
    Given I am on the sprints page
    And a sprint "Sprint 1" in "Active" status exists
    When I complete the sprint "Sprint 1"
    Then the sprint status should change to "Completed"

@sprints @negative
Scenario: Create sprint with end date before start date
    Given I am on the sprints page
    When I create a new sprint with the following details:
        | Field       | Value          |
        | Project     | Test Project   |
        | Name        | Invalid Sprint |
        | Goal        | Test           |
        | Start Date  | 2024-03-14     |
        | End Date    | 2024-03-01     |
    Then I should see a validation error
    And the sprint should not be created

@sprints
Scenario: Cancel sprint creation
    Given I am on the sprints page
    When I click create sprint button
    And I fill in the sprint form partially
    And I click the cancel button
    Then the sprint creation modal should close
    And no new sprint should be created

@sprints @authorization
Scenario: Developer can view but not create sprints
    Given I am logged in as "venkateshboyapati96@gmail.com" with password "Govinda@1117"
    And I am on the sprints page
    Then I should see the sprints list
    But I should not see the create sprint button
