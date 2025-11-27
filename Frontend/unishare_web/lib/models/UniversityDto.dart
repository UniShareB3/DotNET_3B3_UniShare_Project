class UniversityDto {
  final String id;
  final String name;
  final String shortCode;
  final String emailDomain;

  UniversityDto({
    required this.id,
    required this.name,
    required this.shortCode,
    required this.emailDomain,
  });

  factory UniversityDto.fromJson(Map<String, dynamic> json) {
    return UniversityDto(
      id: json['id'],
      name: json['name'],
      shortCode: json['shortCode'],
      emailDomain: json['emailDomain'],
    );
  }
}
