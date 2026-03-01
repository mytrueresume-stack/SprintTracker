Feature: User Authentication
    As a user of SprintTracker
    I want to be able to register, login and logout
    So that I can access the application securely

Background:
    Given the application is running

@authentication @smoke
Scenario: Successful user registration with Developer role
    Given I am on the register page
    When I register with the following details:
        | Field          | Value                    |
        | First Name     | John                     |
        | Last Name      | Developer                |
        | Email          | john.dev@example.com     |
        | Password       | Dev@12345                |
        | Confirm Password | Dev@12345              |
        | Role           | Developer                |
    Then I should be redirected to the dashboard
    And I should see a welcome message

@authentication @smoke
Scenario: Successful user registration with Manager role
    Given I am on the register page
    When I register with the following details:
        | Field            | Value                    |
        | First Name       | Jane                     |
        | Last Name        | Manager                  |
        | Email            | jane.mgr@example.com     |
        | Password         | Mgr@12345                |
        | Confirm Password | Mgr@12345                |
        | Role             | Manager                  |
    Then I should be redirected to the dashboard
    And I should see a welcome message

@authentication
Scenario: Successful user login
    Given I am on the login page
    When I login with email "venkateshboyapati96@gmail.com" and password "Govinda@1117"
    Then I should be redirected to the dashboard
    And I should see a welcome message

@authentication @negative
Scenario: Login with invalid credentials
    Given I am on the login page
    When I login with email "invalid@example.com" and password "WrongPassword"
    Then I should see an error message
    And I should remain on the login page

@authentication @negative
Scenario: Registration with mismatched passwords
    Given I am on the register page
    When I register with the following details:
        | Field            | Value                    |
        | First Name       | Test                     |
        | Last Name        | User                     |
        | Email            | test@example.com         |
        | Password         | Pass@123                 |
        | Confirm Password | DifferentPass@123        |
        | Role             | Developer                |
    Then I should see an error message
    And I should remain on the register page

@authentication @negative
Scenario: Registration with existing email
    Given I am on the register page
    When I register with the following details:
        | Field            | Value                         |
        | First Name       | Test                          |
        | Last Name        | User                          |
        | Email            | venkateshboyapati96@gmail.com |
        | Password         | Govinda@1117                  |
        | Confirm Password | Govinda@1117                  |
        | Role             | Admin                         |
    Then I should see an error message
    And I should remain on the register page

@authentication
Scenario: Navigate from Login to Register page
    Given I am on the login page
    When I click on the register link
    Then I should be on the register page

@authentication
Scenario: Navigate from Register to Login page
    Given I am on the register page
    When I click on the login link
    Then I should be on the login page

@authentication
Scenario: Successful logout
    Given I am logged in as "venkateshboyapati96@gmail.com" with password "Govinda@1117"
    And I am on the dashboard
    When I click the logout button
    Then I should be redirected to the login page
