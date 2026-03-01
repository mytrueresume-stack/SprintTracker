Feature: Project Management
    As a Manager or Admin user
    I want to manage projects
    So that I can organize work into logical units

Background:
Given the application is running
And I am logged in as "venkateshboyapati96@gmail.com" with password "Govinda@1117"

@projects @smoke
Scenario: Create a new project successfully
    Given I am on the projects page
    When I create a new project with the following details:
        | Field       | Value                                    |
        | Name        | E-Commerce Platform                      |
        | Key         | ECOM                                     |
        | Description | Online shopping platform with payments   |
    Then the project should be created successfully
    And I should see the project "E-Commerce Platform" in the projects list

@projects
Scenario: View projects list
    Given I am on the projects page
    Then I should see the projects page
    And I should see a list of projects or empty state

@projects
Scenario: Create project with start and end dates
    Given I am on the projects page
    When I create a new project with the following details:
        | Field       | Value                          |
        | Name        | Mobile App                     |
        | Key         | MOBILE                         |
        | Description | iOS and Android app            |
        | Start Date  | 2024-03-01                     |
        | End Date    | 2024-06-30                     |
    Then the project should be created successfully
    And I should see the project "Mobile App" in the projects list

@projects @negative
Scenario: Create project with duplicate key
    Given I am on the projects page
    And a project with key "ECOM" already exists
    When I attempt to create a project with key "ECOM"
    Then I should see an error message
    And the project should not be created

@projects
Scenario: Search for projects
    Given I am on the projects page
    And multiple projects exist
    When I search for "E-Commerce"
    Then I should see only projects matching "E-Commerce"

@projects
Scenario: Cancel project creation
    Given I am on the projects page
    When I click create project button
    And I fill in the project form partially
    And I click the cancel button
    Then the project creation modal should close
    And no new project should be created

@projects @authorization
Scenario: Developer cannot create projects
    Given I am logged in as "venkateshboyapati96@gmail.com" with password "Govinda@1117"
    And I am on the projects page
    Then I should not see the create project button
