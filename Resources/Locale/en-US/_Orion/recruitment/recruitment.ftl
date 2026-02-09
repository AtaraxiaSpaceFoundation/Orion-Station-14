# Popups
recruitment-start-user-temporal-popup = Hand over the device to { $target } for registration.

recruitment-start-user = You are starting to enter data about { $target } into the device.
recruitment-start-target = { $user } is inviting you to join the organization.

recruitment-success = { $target } is now part of the organization!
recruitment-decline = { $target } has declined to join!
recruitment-already = { $target } is already in the database!
recruitment-failed = { $target } cannot be part of the organization!
recruitment-too-far = The target is too far away!

# UI strings
recruitment-ui-title = Organization Invitation
recruitment-ui-invitation = You are being invited to join the organization!
recruitment-ui-organization = Organization:
recruitment-ui-implant = Implantation:
recruitment-ui-warning = ❗ WARNING ❗
recruitment-ui-warning-text = By joining { $organization }, you will receive the { $implant }. This action is irreversible!
recruitment-ui-accept = Sign
recruitment-ui-decline = Decline

recruitment-list-ui-title = Organization Database
recruitment-member-list-organization = Manifest of { $organization }
recruitment-member-list-count = Total count: { $count }
recruitment-member-list-empty = No members found!

# Table headers
recruitment-member-list-header-name = Name
recruitment-member-list-header-recruiter = Recruiter
recruitment-member-list-header-time = Tenure
recruitment-member-list-unknown = Unknown

# Time formatting
recruitment-member-list-time = { $minutes } { $minutes ->
        [1] minute
        [few] minutes
       *[other] minutes
    } and { $seconds } { $seconds ->
        [1] second
        [few] seconds
       *[other] seconds
    }
